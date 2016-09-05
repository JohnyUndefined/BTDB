using System.Collections.Generic;
using BTDB.StreamLayer;

namespace BTDB.ODBLayer
{
    public class DBWriterCtx : IDBWriterCtx
    {
        readonly IInternalObjectDBTransaction _transaction;
        readonly AbstractBufferedWriter _writer;
        readonly bool _preferInline;
        Dictionary<object, int> _objectIdMap;
        int _lastId = 1; // Skip Zero inline index due to backward compatibility

        public DBWriterCtx(IInternalObjectDBTransaction transaction, AbstractBufferedWriter writer, bool preferInline)
        {
            _transaction = transaction;
            _writer = writer;
            _preferInline = preferInline;
        }

        public bool WriteObject(object @object)
        {
            return CommonWriteObject(@object, false, true);
        }

        bool CommonWriteObject(object @object, bool autoRegister, bool allowInline)
        {
            if (@object == null)
            {
                _writer.WriteVInt64(0);
                return false;
            }
            var oid = _transaction.StoreIfNotInlined(@object, autoRegister, allowInline && _preferInline);
            if (oid != ulong.MaxValue)
            {
                _writer.WriteVInt64((long)oid);
                return false;
            }
            if (_objectIdMap == null) _objectIdMap = new Dictionary<object, int>();
            int cid;
            if (_objectIdMap.TryGetValue(@object, out cid))
            {
                _writer.WriteVInt64(-cid);
                return false;
            }
            _lastId++;
            _objectIdMap.Add(@object, _lastId);
            _writer.WriteVInt64(-_lastId);
            return true;
        }

        public void WriteNativeObject(object @object)
        {
            if (!CommonWriteObject(@object, true, true)) return;
            _transaction.WriteInlineObject(@object, this);
        }

        public void WriteNativeObjectPreventInline(object @object)
        {
            if (!CommonWriteObject(@object, true, false)) return;
            _transaction.WriteInlineObject(@object, this);
        }

        public AbstractBufferedWriter Writer()
        {
            return _writer;
        }

        public int RegisterInstance(object content)
        {
            return ((IInstanceRegistry)_transaction.Owner).RegisterInstance(content);
        }

        public object FindInstance(int id)
        {
            return ((IInstanceRegistry)_transaction.Owner).FindInstance(id);
        }

        public IInternalObjectDBTransaction GetTransaction()
        {
            return _transaction;
        }
    }
}