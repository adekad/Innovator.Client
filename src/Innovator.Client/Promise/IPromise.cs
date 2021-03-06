﻿using System;

namespace Innovator.Client
{
  /// <summary>
  /// Represents a promise that a result will be provided at some point in the future
  /// </summary>
  /// <remarks>
  /// A promise is very similar to a <see cref="System.Threading.Tasks.Task"/> and can be awaited
  /// just like a task.  The API of a promise is very similar to that of a 
  /// <a href="http://api.jquery.com/category/deferred-object/">JQuery Promise</a>
  /// </remarks>
  public interface IPromise : ICancelable
  {
    /// <summary>Whether an error occurred causing the promise to be rejected</summary>
    bool IsRejected { get; }
    /// <summary>Whether the promise completed successfully</summary>
    bool IsResolved { get; }
    /// <summary>The progress of the promise represented as an integer from 0 to 100</summary>
    int PercentComplete { get; }
    /// <summary>The result of the promise.  Only valid if <see cref="IPromise.IsResolved"/> is <c>true</c></summary>
    object Value { get; }

    /// <summary>Callback to be executed when the promise completes regardless of whether an error occurred</summary>
    /// <param name="callback">Callback to be executed</param>
    /// <returns>The current instance for chaining additional calls</returns>
    IPromise Always(Action callback);
    /// <summary>Callback to be executed when the promise completes successfully</summary>
    /// <param name="callback">Callback to be executed with the result of the promise</param>
    /// <returns>The current instance for chaining additional calls</returns>
    IPromise Done(Action<object> callback);
    /// <summary>Callback to be executed when the promise encounters an error</summary>
    /// <param name="callback">Callback to be executed with the exception of the promise</param>
    /// <returns>The current instance for chaining additional calls</returns>
    IPromise Fail(Action<Exception> callback);
    /// <summary>Callback to be executed when the reported progress changes</summary>
    /// <param name="callback">Callback to be executed with the progress [0, 100] and the message</param>
    /// <returns>The current instance for chaining additional calls</returns>
    IPromise Progress(Action<int, string> callback);
  }

  /// <summary>
  /// Represents a promise that a result of the specified type will be provided at some point in the future
  /// </summary>
  /// <remarks>
  /// A promise is very similar to a <see cref="System.Threading.Tasks.Task"/> and can be awaited
  /// just like a task.  The API of a promise is very similar to that of a 
  /// <a href="http://api.jquery.com/category/deferred-object/">JQuery Promise</a>
  /// </remarks>
  public interface IPromise<T> : IPromise
  {
    /// <summary>The result of the promise.  Only valid if <see cref="IPromise.IsResolved"/> is <c>true</c></summary>
    new T Value { get; }

    /// <summary>Callback to be executed when the promise completes regardless of whether an error occurred</summary>
    /// <param name="callback">Callback to be executed</param>
    /// <returns>The current instance for chaining additional calls</returns>
    new IPromise<T> Always(Action callback);
    /// <summary>Callback to be executed when the promise encounters an error</summary>
    /// <param name="callback">Callback to be executed with the exception of the promise</param>
    /// <returns>The current instance for chaining additional calls</returns>
    new IPromise<T> Fail(Action<Exception> callback);
    /// <summary>Callback to be executed when the reported progress changes</summary>
    /// <param name="callback">Callback to be executed with the progress [0, 100] and the message</param>
    /// <returns>The current instance for chaining additional calls</returns>
    new IPromise<T> Progress(Action<int, string> callback);
    /// <summary>Callback to be executed when the promise completes successfully</summary>
    /// <param name="callback">Callback to be executed with the result of the promise</param>
    /// <returns>The current instance for chaining additional calls</returns>
    IPromise<T> Done(Action<T> callback);
  }
}
