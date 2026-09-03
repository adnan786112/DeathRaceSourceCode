using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Unity.Netcode
{ 
public enum ObjectType
{
   MinigunBullet,Rocket,VisualMinigunBullet,VisualRocket,Decal,BulletHitEffect
}

[Serializable]
public class ObjectPoolInfo
{
    public GameObject ObjectPrefab;
    public int AmountToPool;
    [HideInInspector]public List<GameObject> SubObjectPool;
    public GameObject Container;
    public ObjectType ObjectEnum;
}

    public class MainObjectPooler : MonoBehaviour
    {
        public static MainObjectPooler instance;

        private void Awake()
        {
            instance = this;
        }

        public List<ObjectPoolInfo> MainPool;
        void Start()
        {
            for (int i = 0; i < MainPool.Count; i++)
            {
                FillPoolServerRpc(MainPool[i]);
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
        [ServerRpc]
        public void FillPoolServerRpc(ObjectPoolInfo info)
        {
            for (int i = 0; i < info.AmountToPool; i++)
            {
                GameObject Obj = Instantiate(info.ObjectPrefab,info.Container.transform);
                Obj.SetActive(false);
                info.SubObjectPool.Add(Obj);
            }
        }
        [ServerRpc]
        public GameObject GetObjectPoolByEnumServerRpc(ObjectType type)
        {
            ObjectPoolInfo SelectedPool = GetObjectPoolByTypeRpc(type);
            GameObject instance = null;
            if (SelectedPool != null)
            {

                List<GameObject> pool = SelectedPool.SubObjectPool;

                if (pool.Count > 0)
                {
                    instance = pool[pool.Count - 1];
                    SelectedPool.SubObjectPool.Remove(instance);
                }
                else
                {
                    instance = Instantiate(SelectedPool.ObjectPrefab,SelectedPool.Container.transform);

                }

            }
            return instance;
        }
        [Rpc(SendTo.Everyone)]
        public ObjectPoolInfo GetObjectPoolByTypeRpc(ObjectType type)
        {
            for (int i = 0; i < MainPool.Count; i++)
            {
                if (type == MainPool[i].ObjectEnum)
                {
                    return MainPool[i];
                }
            }
            return null;
        }
        [Rpc(SendTo.Everyone)]
        public void ReturnObjectToPoolRpc(GameObject Obj, ObjectType type)
        {
            if (Obj != null)
            {
                Obj.SetActive(false);
            }
            ObjectPoolInfo SelectedPool = GetObjectPoolByTypeRpc(type);
            List<GameObject> pool = SelectedPool.SubObjectPool;

            if (!pool.Contains(Obj))
            {
                pool.Add(Obj);
            }
        }
    }
}
