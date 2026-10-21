using System;
using System.Collections.Generic;

namespace GameCore
{
    public static partial class MemoryPool
    {
        /// <summary>
        /// 内存池收集器。
        /// </summary>
        private sealed class MemoryCollection
        {
            private readonly Queue<IMemory> memories;
            private readonly Type memoryType;
            private int usingMemoryCount;
            private int acquireMemoryCount;
            private int releaseMemoryCount;
            private int addMemoryCount;
            private int removeMemoryCount;

            public MemoryCollection(Type memoryType)
            {
                memories = new Queue<IMemory>();
                this.memoryType = memoryType;
                usingMemoryCount = 0;
                acquireMemoryCount = 0;
                releaseMemoryCount = 0;
                addMemoryCount = 0;
                removeMemoryCount = 0;
            }

            public Type MemoryType => memoryType;

            public int UnusedMemoryCount => memories.Count;

            public int UsingMemoryCount => usingMemoryCount;

            public int AcquireMemoryCount => acquireMemoryCount;

            public int ReleaseMemoryCount => releaseMemoryCount;

            public int AddMemoryCount => addMemoryCount;

            public int RemoveMemoryCount => removeMemoryCount;

            public T Acquire<T>() where T : class, IMemory, new()
            {
                if (typeof(T) != memoryType)
                {
                    throw new Exception("Type is invalid.");
                }

                usingMemoryCount++;
                acquireMemoryCount++;
                lock (memories)
                {
                    if (memories.Count > 0)
                    {
                        return (T)memories.Dequeue();
                    }
                }

                addMemoryCount++;
                return new T();
            }

            public IMemory Acquire()
            {
                usingMemoryCount++;
                acquireMemoryCount++;
                lock (memories)
                {
                    if (memories.Count > 0)
                    {
                        return memories.Dequeue();
                    }
                }

                addMemoryCount++;
                return (IMemory)Activator.CreateInstance(memoryType);
            }

            public void Release(IMemory memory)
            {
                memory.Clear();
                lock (memories)
                {
                    if (m_EnableStrictCheck && memories.Contains(memory))
                    {
                        throw new Exception("The memory has been released.");
                    }

                    memories.Enqueue(memory);
                }

                releaseMemoryCount++;
                usingMemoryCount--;
            }

            public void Add<T>(int count) where T : class, IMemory, new()
            {
                if (typeof(T) != memoryType)
                {
                    throw new Exception("Type is invalid.");
                }

                lock (memories)
                {
                    addMemoryCount += count;
                    while (count-- > 0)
                    {
                        memories.Enqueue(new T());
                    }
                }
            }

            public void Add(int count)
            {
                lock (memories)
                {
                    addMemoryCount += count;
                    while (count-- > 0)
                    {
                        memories.Enqueue((IMemory)Activator.CreateInstance(memoryType));
                    }
                }
            }

            public void Remove(int count)
            {
                lock (memories)
                {
                    if (count > memories.Count)
                    {
                        count = memories.Count;
                    }

                    removeMemoryCount += count;
                    while (count-- > 0)
                    {
                        memories.Dequeue();
                    }
                }
            }

            public void RemoveAll()
            {
                lock (memories)
                {
                    removeMemoryCount += memories.Count;
                    memories.Clear();
                }
            }
        }
    }
}
