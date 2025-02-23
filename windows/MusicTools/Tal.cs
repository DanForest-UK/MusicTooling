using LanguageExt;
using LanguageExt.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
//using Xunit;
using static LanguageExt.Prelude;
using L = LanguageExt;



/// <summary>
/// Tal monad data
/// </summary>
namespace MusicTools
{
    //Todo sort out

    public abstract class TalInfo
    {
        public readonly string ModelName;
        public readonly IID Id;
        public readonly string Description;

        public TalInfo(string modelName, IID id, string description)
        {
            ModelName = modelName;
            Id = id;
            Description = description;
        }
    }

    public class IID
    {
        public readonly string Value;
        public IID(string value)
        {
            Value = value;
        }
        public static IID Empty => new IID("");
    }

    public static class Helpers
    {
        public static string FormatException(Exception e)
        {
            var msg = new StringBuilder();
            FormatException(e, msg);
            return msg.ToString();
        }

        public static void FormatException(Exception e, StringBuilder msg)
        {
            if (e == null)
            {
                return;
            }
            msg.AppendFormat("{0}\n\n{1}\n\n\n\n", e.Message, e.StackTrace);

            FormatException(e.InnerException);
        }

    }

    public class TalWarning : TalInfo
    {
        public TalWarning(string modelName, IID id, string description)
            : base(modelName, id, description)
        { }
    }

    public class TalError : TalInfo
    {
        public Option<Exception> Exception;

        public TalError(string modelName, IID id, string description)
            : base(modelName, id, description)
        { }

        public TalError(string modelName, IID id, string description, Option<Exception> exception)
            : base(modelName, id, description)
        {
            Exception = exception;
        }
    }

    // Transform And Log monad
    public delegate TalState<A> Tal<A>(TalState state);

    // Transform And Log state
    public partial class TalState
    {
        public readonly Seq<TalError> Errors;
        public readonly Seq<TalWarning> Warnings;
        public readonly bool HasFailed;
        readonly L.HashSet<string> ModelsWithError;
        readonly L.HashSet<(string, IID)> ItemsWithError;
        public readonly Option<IID> CurrentId;
        public readonly Option<string> CurrentModel;
        public readonly Action<TalError> Log;

        public TalState(Seq<TalError> errors, Seq<TalWarning> warnings, bool hasFailed, L.HashSet<string> modelsWithError, L.HashSet<(string, IID)> itemsWithError, Option<IID> currentId, Option<string> currentModel, Action<TalError> log = null)
        {
            Errors = errors;
            Warnings = warnings;
            HasFailed = hasFailed;
            ModelsWithError = modelsWithError;
            ItemsWithError = itemsWithError;
            Log = log;
            CurrentId = currentId;
            CurrentModel = currentModel;
        }

        public bool HasError(string modelName) => ModelsWithError.Contains(modelName);
        public bool HasError(string modelName, IID id) => ItemsWithError.Contains((modelName, id));

        public static TalState Empty =
            new TalState(
                errors: Seq<TalError>(),
                warnings: Seq<TalWarning>(),
                hasFailed: false,
                modelsWithError: new L.HashSet<string>(),
                itemsWithError: new L.HashSet<(string, IID)>(),
                None,
                None);

        public TalState AddErrors(Seq<TalError> errors)
        {
            var nerrors = Errors;
            var modelsWithError = ModelsWithError;
            var itemsWithError = ItemsWithError;

            foreach (var error in errors)
            {
                nerrors = nerrors.Add(error);
                modelsWithError = modelsWithError.AddOrUpdate(error.ModelName);
                itemsWithError = itemsWithError.AddOrUpdate((error.ModelName, error.Id));
                Log?.Invoke(error);
            }

            return With(
                Errors: nerrors,
                ItemsWithError: itemsWithError,
                ModelsWithError: modelsWithError);
        }

        public TalState AddErrors(Seq<string> errors) =>
            AddErrors(errors.Select(e => Tal.Error(CurrentModel.IfNone(""), CurrentId.IfNone(IID.Empty), e)));


        public TalState AddWarnings(Seq<TalWarning> warnings)
        {
            var nwarns = Warnings;
            foreach (var warn in warnings)
            {
                nwarns = nwarns.Add(warn);
            }
            return With(Warnings: nwarns);
        }

        public TalState AddWarnings(Seq<string> warnings) =>
            AddWarnings(warnings.Select(e => Tal.Warn(CurrentModel.IfNone(""), CurrentId.IfNone(IID.Empty), e)));

        public TalState AddException(Exception e) =>
        With(HasFailed: true)
                .AddErrors(Seq1(new TalError("Exception", IID.Empty, Helpers.FormatException(e), e)));

        public TalState With(
            Seq<TalError>? Errors = null,
            Seq<TalWarning>? Warnings = null,
            bool? HasFailed = null,
            L.HashSet<string>? ModelsWithError = null,
            L.HashSet<(string, IID)>? ItemsWithError = null,
            Option<string>? CurrentModel = null,
            Option<IID>? CurrentId = null,
            Action<TalError> Log = null) =>
                new TalState(
                    Errors ?? this.Errors,
                    Warnings ?? this.Warnings,
                    HasFailed ?? this.HasFailed,
                    ModelsWithError ?? this.ModelsWithError,
                    ItemsWithError ?? this.ItemsWithError,
                    CurrentId ?? this.CurrentId,
                    CurrentModel ?? this.CurrentModel,
                    Log ?? this.Log);
    }

    // Transform And Log  monad result
    public class TalState<A>
    {
        public readonly A Value;
        public readonly TalState State;

        public TalState(A value, TalState state)
        {
            Value = value;
            State = state;
        }
    }

    public class TalResult<A>
    {
        public readonly A Value;
        public readonly Seq<TalError> Errors;
        public readonly Seq<TalWarning> Warnings;

        public TalResult(A value, Seq<TalError> errors, Seq<TalWarning> warnings)
        {
            Value = value;
            Errors = errors;
            Warnings = warnings;
        }
    }

    /// <summary>
    /// Transform And Log monad
    /// </summary>
    public static class Tal
    {
        public static TalError Error(string modelName, IID id, string description, Option<Exception> exception = default) =>
         new TalError(modelName, id, description, exception);

        public static TalWarning Warn(string modelName, IID id, string description) =>
            new TalWarning(modelName, id, description);

        public static Tal<A> context<A>(
           string modelName,
           Tal<A> iso) =>
               from _ in setModelName(modelName)
               from r in iso
               from _1 in setModelName("")
               select r;

        public static Tal<A> context<A>(
           IID id,
           Tal<A> iso) =>
               from _ in setId(id)
        from r in iso
               from _1 in setId(IID.Empty)
               select r;

        public static Tal<A> context<A>(
            string modelName,
            IID id,
            Tal<A> iso) =>
               from _ in setInfo(modelName, id)
        from r in iso
               from _1 in setInfo("", IID.Empty)
               select r;

        private static Tal<Unit> setModelName(string modelName) => state =>
            new TalState<Unit>(unit, state.With(CurrentModel: modelName, CurrentId: IID.Empty)); // If you have changed the model name you can be sure the current ID is no longer relevant

        private static Tal<Unit> setId(IID logId) => state =>
            new TalState<Unit>(unit, state.With(CurrentId: logId));

        private static Tal<Unit> setInfo(string modelName, IID logId) => state =>
            new TalState<Unit>(unit, state.With(
                CurrentId: logId,
        CurrentModel: modelName));

        public static Tal<(Option<string> CurrentModelName, Option<IID> CurrentId)> getInfo() => state =>
            new TalState<(Option<string> CurrentModelName, Option<IID> CurrentId)>(
                (CurrentModelName: state.CurrentModel, CurrentId: state.CurrentId),
                state);

        public static Tal<A> success<A>(A value) => state =>
                new TalState<A>(value, state);

        public static Tal<A> warn<A>(A value, Seq<TalWarning> warnings) => state =>
            new TalState<A>(value, state.AddWarnings(warnings));

        public static Tal<A> warn<A>(A value, string warnings) => state =>
            new TalState<A>(value, state.AddWarnings(warnings.Cons().ToSeq()));

        public static Tal<Unit> warn<Unit>(string description) =>
            warn<Unit>(description.Cons().ToSeq());

        public static Tal<Unit> warn<Unit>(Seq<string> warnings) => state =>
            new TalState<Unit>(default, state.AddWarnings(warnings));

        public static Tal<A> warn<A>(A value, Seq<string> warnings) => state =>
            new TalState<A>(value, state.AddWarnings(warnings));

        public static Tal<A> warn<A>(A value, TalWarning warning) =>
            warn(value, warning.Cons().ToSeq());

        public static Tal<A> warn<A>(A value, string logModelName, IID logId, string description) =>
            warn(value, Warn(logModelName, logId, description).Cons().ToSeq());

        public static Tal<A> error<A>(A value, Seq<TalError> errors) => state =>
            new TalState<A>(value, state.AddErrors(errors));

        public static Tal<A> error<A>(A value, Seq<string> errors) => state =>
            new TalState<A>(value, state.AddErrors(errors));

        public static Tal<A> error<A>(A value, string error) => state =>
            new TalState<A>(value, state.AddErrors(error.Cons().ToSeq()));

        public static Tal<Unit> error<Unit>(Seq<TalError> errors) => state =>
            new TalState<Unit>(default, state.AddErrors(errors));

        public static Tal<Unit> error<Unit>(Seq<string> errors) => state =>
            new TalState<Unit>(default, state.AddErrors(errors));

        public static Tal<Unit> error<Unit>(TalError error) =>
            error<Unit>(error.Cons().ToSeq());

        public static Tal<Unit> error<Unit>(string logModelName, IID logId, string description) =>
            error<Unit>(Error(logModelName, logId, description).Cons().ToSeq());

        public static Tal<Unit> error<Unit>(string description) =>
            error<Unit>(description.Cons().ToSeq());

        public static Tal<A> error<A>(A value, TalError error) => state =>
            new TalState<A>(value, state.AddErrors(error.Cons().ToSeq()));

        public static Tal<A> error<A>(A value, string logModelName, IID logId, string description) => state =>
            new TalState<A>(value, state.AddErrors(Error(logModelName, logId, description).Cons().ToSeq()));

        public static Tal<A> fail<A>(Seq<TalError> errors) => state =>
            new TalState<A>(default, state.AddErrors(errors).With(HasFailed: true));

        public static Tal<A> fail<A>(Seq<string> errors) => state =>
            new TalState<A>(default, state.AddErrors(
                errors).With(HasFailed: true));

        public static Tal<A> fail<A>(TalError error) =>
            fail<A>(error.Cons().ToSeq());

        public static Tal<A> fail<A>(string logModelName, IID logId, string description) =>
            fail<A>(Error(logModelName, logId, description).Cons().ToSeq());

        public static Tal<A> fail<A>(string description) =>
            fail<A>(description.Cons().ToSeq());

        public static Tal<Unit> fail<Unit>() =>
            fail<Unit>(new Seq<TalError>());

        public static Tal<(A Result, bool DidFail)> ContinueIfFail<A>(this Tal<A> self, Option<A> defaultValue = default) =>
            state =>
                self.Run(state.Log, state.CurrentModel, state.CurrentId).Match(
                    Left: result => new TalState<(A Result, bool DidFail)>((defaultValue.IfNone(default(A)), true), state.AddErrors(result.Errors).AddWarnings(result.Warnings).With(HasFailed: false)),
                    Right: result => new TalState<(A Result, bool DidFail)>((result.Value, false), state.AddErrors(result.Errors).AddWarnings(result.Warnings).With(HasFailed: false)));

        public static Tal<Unit> failIfErrors() => state =>
            state.Errors.Any()
                ? new TalState<Unit>(unit, state.With(HasFailed: true))
                : new TalState<Unit>(unit, state);

        public static Tal<Unit> failIfErrors(string modelName) => state =>
            state.HasError(modelName)
                ? new TalState<Unit>(unit, state.With(HasFailed: true))
                : new TalState<Unit>(unit, state);

        public static Tal<Unit> failIfErrors(string modelName, IID id) => state =>
            state.HasError(modelName, id)
                ? new TalState<Unit>(unit, state.With(HasFailed: true))
                : new TalState<Unit>(unit, state);

        public static Tal<Unit> FailIfErrors<A>(this Tal<A> self) =>
            from s in self
            from _1 in failIfErrors()
        select unit;

        static TalError ToTalError(this Exception ex, string logModelName, IID logId, Option<string> additionalInfo = default, bool supressException = false) =>
            Error(logModelName, logId, ToTalErrorMessage(ex, additionalInfo, supressException), ex);

        static string ToTalErrorMessage(this Exception ex, Option<string> additionalInfo = default, bool supressException = false) =>
             supressException
                    ? additionalInfo.IfNone("Unknown error")
                    : additionalInfo.Match(
                       Some: ai => $"{ai}: {ex.Message}",
                       None: () => ex.Message);

        public static Tal<Option<B>> TMap<A, B>(this Option<A> option, Func<A, Tal<B>> map) =>
           option.Match(
               Some: value => from m in map(value)
                              select m == null
                                ? None
                                : Some(m),
               None: () => success<Option<B>>(default));

        public static Tal<Unit> TryWithFail(Action a, string logModelName, IID logId, Option<string> additionalInfo = default, bool supressException = false) =>
            Try(() =>
            {
                a();
                return unit;
            }).ToTalFail(ex => ex.ToTalError(logModelName, logId, additionalInfo, supressException));

        public static Tal<Unit> TryWithFail(Action a, Option<string> additionalInfo = default, bool supressException = false) =>
            Try(() =>
            {
                a();
                return unit;
            }).ToTalFail(ex => ex.ToTalErrorMessage(additionalInfo, supressException));

        public static Tal<T> TryWithFail<T>(Func<T> f, string logModelName, IID logId, Option<string> additionalInfo = default, bool supressException = false) =>
            Try(() => f()).ToTalFail(ex => ex.ToTalError(logModelName, logId, additionalInfo, supressException));

        public static Tal<T> TryWithFail<T>(Func<T> f, Option<string> additionalInfo = default, bool supressException = false) =>
            Try(() => f()).ToTalFail(ex => ex.ToTalErrorMessage(additionalInfo, supressException));

        public static Tal<A> ToTalError<A>(this Try<A> self, Func<Exception, string> error) =>
            self.Match(
                Succ: value => success(value),
                Fail: ex => error<A>(error(ex)));

        public static Tal<A> ToTalError<A>(this Try<A> self, Func<Exception, TalError> error) =>
            self.Match(
                Succ: value => success(value),
                Fail: ex => error<A>(error(ex)));

        public static Tal<A> ToTalFail<A>(this Try<A> self, Func<Exception, TalError> error) =>
            self.Match(
                Succ: value => success(value),
                Fail: ex => fail<A>(error(ex)));

        public static Tal<A> ToTalFail<A>(this Try<A> self, Func<Exception, string> error) =>
            self.Match(
                Succ: value => success(value),
                Fail: ex => fail<A>(error(ex)));

        public static Tal<Unit> TryWithError(Action a, string logModelName, IID logId, Option<string> additionalInfo = default) =>
            Try(() =>
            {
                a();
                return unit;
            }).ToTalError(ex => Error(logModelName, logId, additionalInfo.Match(
                   Some: ai => $"{ai}: {ex.Message}",
                   None: () => ex.Message)));

        public static Tal<Unit> TryWithError(Action a, Option<string> additionalInfo = default) =>
            Try(() =>
            {
                a();
                return unit;
            }).ToTalError(ex => ex.ToTalErrorMessage(additionalInfo.Match(
                   Some: ai => $"{ai}: {ex.Message}",
                   None: () => ex.Message)));

        public static Tal<A> ToTal<A>(this A value) => success(value);

        public static Tal<A> TIfNone<A>(this Option<A> option, Tal<A> ifNone) =>
            option.Map(v => success(v)).IfNone(ifNone);

        public static Tal<A> TIfNone<A>(this Option<A> option, Func<Tal<A>> ifNone) =>
            option.Map(success).IfNone(ifNone);

        public static Tal<A> IfNoneFail<A>(this Option<A> option, string logModelName, IID logId, string message) =>
            option.Map(v => success(v)).IfNone(() => fail<A>(Error(logModelName, logId, message)));

        public static Tal<A> IfNoneFail<A>(this Option<A> option, string message) =>
            option.Map(v => success(v)).IfNone(() => fail<A>(message));

        public static Tal<A> IfNoneFail<A>(this Tal<Option<A>> tOption, string message) =>
            from option in tOption
            from value in option.Map(v => success(v)).IfNone(() => fail<A>(message))
        select value;

        public static Tal<A> IfNoneFail<A>(this Tal<Option<A>> tOption, string logModelName, IID logId, string message) =>
            from option in tOption
            from value in option.Map(v => success(v)).IfNone(() => fail<A>(Error(logModelName, logId, message)))
        select value;

        public static Tal<A> IfNoneError<A>(this Option<A> option, string logModelName, IID logId, string message) =>
            option.Map(v => success(v)).IfNone(() => error<A>(Error(logModelName, logId, message)));

        public static Tal<A> IfNoneError<A>(this Option<A> option, string message) =>
            option.Map(v => success(v)).IfNone(() => error<A>(message));

        public static Tal<B> IfLeftError<A, B>(this Either<A, B> either, string logModelName, IID logId, string message) =>
            either.Map(v => success(v)).IfLeft(error<B>(Error(logModelName, logId, message)));

        public static Tal<Unit> IfLeftError(this Either<Error, Unit> either) =>
            either.Match(
                Right: success,
                Left: er => error<Unit>(er.Message));

        public static Tal<B> IfLeftFail<A, B>(this Either<A, B> either, string logModelName, IID logId, string message) =>
            either.Map(v => success(v)).IfLeft(fail<B>(Error(logModelName, logId, message)));

        public static Tal<B> IfLeftError<B>(this Either<Error, B> either, string logModelName, IID logId) =>
            either.Match(
                Right: success,
                Left: err => error<B>(Error(logModelName, logId, err.Message)));

        public static Tal<B> IfLeftFail<B>(this Either<Error, B> either, string logModelName, IID logId) =>
            either.Match(
                Right: success,
                Left: err => fail<B>(Error(logModelName, logId, err.Message)));

        public static Tal<B> IfLeftFail<B>(this Either<Error, B> either) =>
            either.Match(
                Right: success,
                Left: err => fail<B>(err.Message));

        public static Tal<A> toTal<A>(this Seq<TalError> errors, A value) => state =>
            new TalState<A>(value, state.AddErrors(errors));

        public static Tal<A> toTal<A>(this Seq<TalWarning> warnings, A value) => state =>
            new TalState<A>(value, state.AddWarnings(warnings));

        public static Tal<Unit> toTal(this Seq<TalWarning> warnings) => state =>
            new TalState<Unit>(default, state.AddWarnings(warnings));

        public static Tal<Unit> toTal(this Seq<TalError> errors) => state =>
            new TalState<Unit>(default, state.AddErrors(errors));

        public static Tal<A> toTal<A>(this TalError error, A value) => state =>
            new TalState<A>(value, state.AddErrors(error.Cons().ToSeq()));

        public static Tal<A> toTal<A>(this TalWarning warning, A value) => state =>
            new TalState<A>(value, state.AddWarnings(warning.Cons().ToSeq()));

        public static Tal<Unit> toTal(this TalWarning warning) => state =>
            new TalState<Unit>(default, state.AddWarnings(warning.Cons().ToSeq()));

        public static Tal<Unit> toTal(TalError error) => state =>
            new TalState<Unit>(default, state.AddErrors(error.Cons().ToSeq()));

        public static Tal<Unit> FailIf(bool condition, string message) =>
            condition
                ? fail<Unit>(message)
                : success(unit);

        public static Tal<Unit> FailIf(Func<bool> condition, string message) =>
            condition()
                ? fail<Unit>(message)
                : success(unit);

        public static Tal<Unit> FailIf(bool condition, string logModelName, IID logId, string message) =>
            condition
                ? fail<Unit>(logModelName, logId, message)
                : success(unit);

        public static Tal<Unit> FailIf(Func<bool> condition, string logModelName, IID logId, string message) =>
            condition()
                ? fail<Unit>(logModelName, logId, message)
                : success(unit);

        public static Tal<Unit> ErrorIf(bool condition, string message) =>
             condition
                ? error<Unit>(message)
                : success(unit);

        public static Tal<Unit> ErrorIf(bool condition, string logModelName, IID logId, string message) =>
            condition
                ? error<Unit>(logModelName, logId, message)
                : success(unit);

        public static Tal<Unit> WarnIf(bool condition, string message) =>
            condition
                ? warn<Unit>(message)
                : success(unit);

        public static Tal<Unit> WarnIf(bool condition, string logModelName, IID logId, string message) =>
            condition
                ? warn<Unit>(unit, logModelName, logId, message)
                : success(unit);

        public static Tal<B> Bind<A, B>(this Tal<A> ma, Func<A, Tal<B>> f) => state =>
        {
            if (state.HasFailed)
                return new TalState<B>(default, state);
            if (ma == null || f == null)
                return new TalState<B>(default, state.AddException(new Exception("Tal instance is null")));
            var ra = ma(state);
            if (ra.State.HasFailed)
                return new TalState<B>(default, ra.State);
            Tal<B> mb = null;
            try
            {
                mb = f(ra.Value);
            }
            catch (Exception e)
            {
                return new TalState<B>(default, ra.State.AddException(e));
            }

            return mb(ra.State);
        };

        public static Tal<B> SelectMany<A, B>(this Tal<A> ma, Func<A, Tal<B>> f) =>
            Bind(ma, f);

        public static Tal<B> Map<A, B>(this Tal<A> ma, Func<A, B> f) =>
            ma.Bind(a => success(f(a)));

        public static Tal<B> Select<A, B>(this Tal<A> ma, Func<A, B> f) =>
            ma.Bind(a => success(f(a)));

        public static Tal<C> SelectMany<A, B, C>(this Tal<A> ma, Func<A, Tal<B>> bind, Func<A, B, C> project) =>
            ma.Bind(a => bind(a).Map(b => project(a, b)));

        public static Either<TalResult<Unit>, TalResult<A>> Run<A>(this Tal<A> computation, Action<TalError> logError = null, Option<string> currentModel = default, Option<IID> currentId = default)
        {
            var res = computation(TalState.Empty.With(
                Log: logError,
                CurrentId: currentId,
                CurrentModel: currentModel));

            return res.State.HasFailed
                ? Left<TalResult<Unit>, TalResult<A>>(new TalResult<Unit>(unit, res.State.Errors.ToSeq(), res.State.Warnings.ToSeq()))
                : Right<TalResult<Unit>, TalResult<A>>(new TalResult<A>(res.Value, res.State.Errors.ToSeq(), res.State.Warnings.ToSeq()));
        }

        public static (Seq<TalError> Errors, Seq<TalWarning> Warnings) Validate(this Tal<Unit> computation, Action<TalError> logError = null)
        {
            var res = computation(TalState.Empty.With(Log: logError));
            return (res.State.Errors.ToSeq(), res.State.Warnings.ToSeq());
        }

        /// <summary>Flips the sequence of Tals to be a Tal of Sequences</summary>
        public static Tal<Seq<A>> Sequence<A>(this IEnumerable<Tal<A>> mas) => state =>
        {
            var rs = new A[mas.Count()];
            int index = 0;

            foreach (var ma in mas)
            {
                var s = ma(state);
                if (s.State.HasFailed)
                {
                    return new TalState<Seq<A>>(default, s.State);
                }

                state = s.State;
                rs[index] = s.Value;
                index++;
            }
            return new TalState<Seq<A>>(rs.ToSeq(), state);
        };

        public static Tal<Option<A>> Sequence<A>(this Option<Tal<A>> ma) =>
            ma.Match(
                Some: tx => tx.Map(x => Some(x)),
                None: state => new TalState<Option<A>>(Option<A>.None, state));

        public static Tal<Option<B>> Traverse<A, B>(this Option<A> ma, Func<A, Tal<B>> f) =>
            ma.Match(
                Some: a => f(a).Map(x => Some(x)),
                None: state => new TalState<Option<B>>(Option<B>.None, state));

        /// <summary>Binds sequence of unit Tal regardless if one fails.</summary>
        /// <remarks>Similar function to Sequence but does not 'early out'.</remarks>
        public static Tal<Unit> BindMany(this IEnumerable<Tal<Unit>> mas) => state =>
        {
            bool hasFailed = false;
            foreach (var ma in mas)
            {
                var s = ma(state);
                if (s.State.HasFailed)
                {
                    hasFailed = true;
                }
                state = s.State.With(HasFailed: false);
            }
            return new TalState<Unit>(unit, state.With(HasFailed: hasFailed));
        };

        /// <summary>Binds sequence of unit Tal in parallel regardless if one fails.</summary>
        /// <remarks>Similar function to Sequence but does not 'early out'.</remarks>
        public static Tal<Unit> RunParallel(this IEnumerable<Tal<Unit>> mas, Option<int> maxDegreesOfParallelism = default, bool caching = false) => state =>
        {
            var resultCollection = new ConcurrentBag<Either<TalResult<Unit>, TalResult<Unit>>>();

            if (mas.Count() == 0)
            {
                return new TalState<Unit>(unit, state);
            }

            // If the Tal does any caching, run the first item alone so the work is not duplicated
            // in several threads
            if (caching)
            {
                resultCollection.Add(mas.First().Run(state.Log, state.CurrentModel, state.CurrentId));
                mas = mas.Tail();
            }

            var degrees = maxDegreesOfParallelism.IfNone(() => Environment.ProcessorCount);
            var options = new ParallelOptions();
            options.MaxDegreeOfParallelism = degrees;

            Parallel.ForEach(mas, options, item => resultCollection.Add(item.Run(state.Log, state.CurrentModel, state.CurrentId)));

            var failed = resultCollection.Where(r => r.IsLeft).Any();

            var errors = from r in resultCollection
                         from err in r.ExtractErrors().Errors
                         select err;

            var warnings = from r in resultCollection
                           from warning in r.ExtractErrors().Warnings
                           select warning;

            return new TalState<Unit>(unit, state.With(HasFailed: failed)
                .AddErrors(errors.ToSeq()).AddWarnings(warnings.ToSeq()));
        };

        public static (Seq<TalError> Errors, Seq<TalWarning> Warnings) ExtractErrors<A>(this Either<TalResult<Unit>, TalResult<A>> result) =>
            result.Match(
                Left: v => (v.Errors, v.Warnings),
                Right: v => (v.Errors, v.Warnings));

        public static Seq<TalInfo> ExtractInfo<A>(this Either<TalResult<Unit>, TalResult<A>> result) =>
           result.Match(
               Left: v => new Seq<TalInfo>().Concat(v.Warnings).Concat(v.Errors),
               Right: v => new Seq<TalInfo>().Concat(v.Warnings).Concat(v.Errors));

        public static A IfLeftThrow<A>(this Tal<A> tal) =>
          tal.Run().IfLeft(r => throw r.Errors.ToException()).Value;

        public static A IfErrorsThrow<A>(this Tal<A> tal) =>
             tal.Run().Match(
                 Left: r => raise<A>(r.Errors.ToException()),
                 Right: r => r.Errors.Any()
                    ? raise<A>(r.Errors.ToException())
                    : r.Value);

        public static Exception ToException(this Seq<TalError> errors) =>
            new Exception(string.Join(", ", errors.Select(d => d.Description)));

        public static IID noid => IID.Empty;
    }
}
