namespace Suave.Sockets

open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.Net.Sockets

type ConcurrentPool<'T>() =

  let pool = new ConcurrentStack<'T>()

  member x.Push(item : 'T) =
    pool.Push item

  member x.Pop() =
    match pool.TryPop() with
    | true, v   -> v
    | false, _  -> Unchecked.defaultof<'T>
