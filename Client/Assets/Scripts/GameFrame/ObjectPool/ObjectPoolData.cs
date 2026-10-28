using System.Collections.Generic;
using UnityEngine;
using GameCore;

namespace GameFrame
{
    public class ObjectPoolData
    {
        public readonly GameObject m_Father;
        public readonly Queue<GameObject> m_Pool;

        public ObjectPoolData(GameObject gameObject, GameObject root)
        {
            m_Father = new GameObject($"{gameObject.name}.Pool");
            m_Father.transform.SetParent(root.transform);
            m_Pool = new Queue<GameObject>();
            PushGameObject(gameObject);
        }

        public ObjectPoolData(GameObject father)
        {
            this.m_Father = father;
        }

        public void PushGameObject(GameObject gameObject)
        {
            m_Pool.Enqueue(gameObject);
            gameObject.transform.SetParent(m_Father.transform);
            gameObject.SetActive(false);
        }

        public GameObject GetGameObject()
        {
            GameObject go = m_Pool.Dequeue();
            go.SetActive(true);
            return go;
        }
    }
}
