namespace Suave.Sockets

open System
open System.Collections.Generic
open System.Collections.Concurrent

open Suave

/// This class creates a single large buffer which can be divided up
/// and assigned to SocketAsyncEventArgs objects for use with each
/// socket I/O operation.
/// This enables bufffers to be easily reused and guards against
/// fragmenting heap memory.
///
/// The operations exposed on the BufferManager class are not thread safe.
[<AllowNullLiteral>]
type BufferManager(totalBytes, bufferSize, logger) =
  do Log.internf logger "Suave.Socket.BufferManager" (fun fmt ->
    fmt "initialising BufferManager with %d bytes" totalBytes)

  /// the underlying byte array maintained by the Buffer Manager
  let buffer = Array.zeroCreate totalBytes
  let freeOffsets = new ConcurrentStack<int>()

  /// Pops a buffer from the buffer pool
  member x.PopBuffer(?context : string) : ArraySegment<byte> =
    let success, offset = freeOffsets.TryPop()
    if not success then
        Log.internf logger "Suave.Socket.BufferManager" (fun fmt ->
            fmt "cound not POP reserving buffer: %d [%s]" offset (defaultArg context "no-ctx"))
        failwith "PopBuffer FAILED - out of buffers, need to increase maxOps at startup" // TODO dynamically resize
    else
        Log.internf logger "Suave.Socket.BufferManager" (fun fmt ->
            fmt "reserving buffer: %d [%s]" offset (defaultArg context "no-ctx"))
    ArraySegment(buffer, offset, bufferSize)

  /// Initialise the memory required to use this BufferManager
  member x.Init() =
    let mutable runningOffset = 0
    while runningOffset < totalBytes - bufferSize do
      freeOffsets.Push runningOffset
      runningOffset <- runningOffset + bufferSize

  /// Frees the buffer back to the buffer pool
  member x.FreeBuffer(args : ArraySegment<_>, ?context : string) =
    // if freeOffsets.Contains args.Offset then failwithf "double free buffer %d" args.Offset
      
    freeOffsets.Push args.Offset
    //let count = freeOffsets.Count // may change since Push 

    //Log.internf logger "Suave.Socket.BufferManager" (fun fmt ->  fmt "freeing buffer: %d, free count: %d [%s]" args.Offset count (defaultArg context "no-ctx"))
