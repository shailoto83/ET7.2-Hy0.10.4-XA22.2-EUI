using System.Collections.Generic;
using UnityEngine;

namespace xasset
{
    public class Scheduler : MonoBehaviour
    {
        private static readonly Queue<Request> Queue = new Queue<Request>();
        private static readonly List<Request> Processing = new List<Request>();
        private static float _realtimeSinceStartup;

        [SerializeField] [Tooltip("最大单帧更新数量。")] [Range(2, 10)]
        private byte maxUpdateCount = 10;

        [SerializeField] [Tooltip("最大单帧更新时间片，值越大处理的请求数量越多，值越小处理请求的数量越小，可以根据目标帧率分配。")]
        private float maxUpdateTimeSlice = 1 / 60f;

        public static bool Working => Processing.Count > 0 || Queue.Count > 0;
        public static bool Busy => Time.realtimeSinceStartup - _realtimeSinceStartup > MaxUpdateTimeSlice;
        public static float MaxUpdateTimeSlice { get; set; }
        public static byte MaxUpdateCount { get; set; } = 10;

        private void Start()
        {
            MaxUpdateTimeSlice = maxUpdateTimeSlice;
            MaxUpdateCount = maxUpdateCount;
        }

        private void Update()
        {
            _realtimeSinceStartup = Time.realtimeSinceStartup;
            while (Queue.Count > 0 && (Processing.Count < MaxUpdateCount || MaxUpdateCount == 0))
            {
                var item = Queue.Dequeue();
                Processing.Add(item);
                if (item.status == Request.Status.Wait) item.Start();
                if (Busy) return;
            }

            for (var index = 0; index < Processing.Count; index++)
            {
                var item = Processing[index];
                if (item.Update()) continue;
                Processing.RemoveAt(index);
                index--;
                item.Complete();
                if (Busy) return;
            }
        }

        public static void Enqueue(Request request)
        {
            Queue.Enqueue(request);
        }
    }
}