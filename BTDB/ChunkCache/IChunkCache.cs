﻿using System.Threading.Tasks;
using BTDB.Buffer;

namespace BTDB.ChunkCache
{
    interface IChunkCache
    {
        void Put(ByteBuffer key, ByteBuffer content);
        Task<ByteBuffer> Get(ByteBuffer key);
    }
}
