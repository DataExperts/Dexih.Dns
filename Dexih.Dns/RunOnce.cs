// using System;
// using System.Threading;
// using System.Threading.Tasks;
//
// namespace Dexih.Dns
// {
//     public class RunOnce<T>
//     {
//         private  T _value;
//         private bool _running;
//         private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(0);
//         private readonly object _lock = 1;
//
//         public async Task<T> RunAsync(Func<Task<T>> func)
//         {
//             bool running;
//             lock (_lock)
//             {
//                 running = _running;
//                 if (!running)
//                 {
//                     _running = true;
//                 }
//             }
//             
//             if (running)
//             {
//                 await _semaphoreSlim.WaitAsync();
//                 _semaphoreSlim.Release();
//                 return _value;
//             }
//
//             try
//             {
//                 _value = await func.Invoke();
//                 _running = false;
//                 return _value;
//             }
//             finally
//             {
//                 _semaphoreSlim.Release();
//             }
//         }
//     }
// }