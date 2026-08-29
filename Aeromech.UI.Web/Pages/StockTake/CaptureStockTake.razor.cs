using AeroMech.Data.Enums;
using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Globalization;

namespace AeroMech.UI.Web.Pages.StockTake
{
    public partial class CaptureStockTake
    {
        [Inject] private StockTakeService _stockTakeService { get; set; } = default!;
        [Inject] private LoaderService _loaderService { get; set; } = default!;
        [Inject] private ConfirmationService _confirmationService { get; set; } = default!;
        [Inject] private NavigationManager _navigationManager { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] protected BlazorBootstrap.ToastService ToastService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider _authenticationStateProvider { get; set; } = default!;

        [Parameter] public int Id { get; set; }

        private enum ScreenMode { Count, Review }

        private enum LineFilter { ToCount, Counted, Recount, All }

        private enum ReviewFilter { Differences, Unsettled, Accepted, AllLines }

        private StockTakeModel? _stockTake;
        private bool _notFound;

        private ScreenMode _mode = ScreenMode.Count;
        private LineFilter _filter = LineFilter.ToCount;
        private ReviewFilter _reviewFilter = ReviewFilter.Differences;
        private StockTakeSheetOrder _order = StockTakeSheetOrder.SupplierThenPart;

        private string _currentUser = string.Empty;

        private const int PageSize = 50;
        private int _page = 1;

        private string? _saveState;
        private bool _saveFailed;

        /// <summary>
        /// The lines the current filter admitted when it was applied. Filtering live would pull a
        /// row out from under the cursor the moment a figure was typed into it, which is exactly
        /// when a counter is least able to cope with the list moving.
        /// </summary>
        private readonly HashSet<int> _frozenFilterIds = new();

        private string _searchTerm = string.Empty;

        private string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (_searchTerm == value) return;
                _searchTerm = value ?? string.Empty;
                _page = 1;
            }
        }

        /// <summary>
        /// A posted or cancelled sheet is a record, not a working document.
        /// </summary>
        private bool IsReadOnly => _stockTake is null || !_stockTake.IsOpen;

        /// <summary>
        /// Whether the counting grid shows what the system expects. Held back on a blind count for
        /// the same reason it is held back on the printed sheet.
        /// </summary>
        private bool ShowExpected => _stockTake is not null && !_stockTake.BlindCount;

        /// <summary>
        /// A line settled in review is not editable back on the counting grid. The service refuses
        /// such a write anyway, to stop a stray save undoing a reviewer's decision; leaving the box
        /// live would let a figure be typed that was then quietly dropped.
        /// </summary>
        private bool IsLineLocked(StockTakeLineModel line)
            => IsReadOnly || line.Status == StockTakeLineStatus.Accepted;

        /// <summary>
        /// Parts that moved since the sheet froze their level, which is only a question worth
        /// asking while the sheet is open. Once posted, the correction itself is what makes the
        /// live level differ from the snapshot, so every adjusted line would otherwise report
        /// itself as having moved.
        /// </summary>
        private int MovedDuringCountCount
            => IsReadOnly ? 0 : _stockTake?.Lines.Count(x => x.HasMovedSinceSnapshot) ?? 0;

        protected override async Task OnParametersSetAsync()
        {
            // The route parameter changes when moving between sheets without leaving the page.
            if (_stockTake is not null && _stockTake.Id == Id) return;

            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            _currentUser = state.User.Identity?.Name ?? string.Empty;

            await LoadStockTake();
        }

        private async Task LoadStockTake()
        {
            _loaderService.ShowLoader();
            try
            {
                _stockTake = await _stockTakeService.GetStockTake(Id, _order);
                _notFound = _stockTake is null;

                if (_stockTake is null) return;

                // A sheet already in review, or one finished, opens on the differences: that is the
                // work left on it.
                _mode = _stockTake.Status is StockTakeStatus.Review or StockTakeStatus.Completed or StockTakeStatus.Cancelled
                    ? ScreenMode.Review
                    : ScreenMode.Count;

                if (IsReadOnly)
                    _reviewFilter = ReviewFilter.Differences;

                FreezeFilter();
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }

        // ---------------------------------------------------------------------
        // Filtering and paging
        // ---------------------------------------------------------------------

        private IEnumerable<StockTakeLineModel> FilteredLines
        {
            get
            {
                if (_stockTake is null) return Enumerable.Empty<StockTakeLineModel>();

                var lines = _stockTake.Lines.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(_searchTerm))
                {
                    var t = _searchTerm.Trim();
                    lines = lines.Where(x =>
                        (x.PartCode ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                        (x.PartDescription ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                        (x.Bin ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                        (x.SupplierCode ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase));
                }

                lines = _mode == ScreenMode.Count
                    ? ApplyCountFilter(lines)
                    : ApplyReviewFilter(lines);

                // Review works down what the differences are worth, because a unit out on a cheap
                // washer and on an expensive part are not the same problem.
                if (_mode == ScreenMode.Review)
                    lines = lines.OrderByDescending(x => x.AbsoluteVarianceValue).ThenBy(x => x.PartCode, StringComparer.OrdinalIgnoreCase);

                return lines;
            }
        }

        private IEnumerable<StockTakeLineModel> ApplyCountFilter(IEnumerable<StockTakeLineModel> lines) => _filter switch
        {
            LineFilter.ToCount => lines.Where(x => !x.IsCounted || _frozenFilterIds.Contains(x.Id)),
            LineFilter.Counted => lines.Where(x => x.IsCounted || _frozenFilterIds.Contains(x.Id)),
            LineFilter.Recount => lines.Where(x => x.Status == StockTakeLineStatus.RecountRequested || _frozenFilterIds.Contains(x.Id)),
            _ => lines
        };

        private IEnumerable<StockTakeLineModel> ApplyReviewFilter(IEnumerable<StockTakeLineModel> lines) => _reviewFilter switch
        {
            ReviewFilter.Differences => lines.Where(x => x.HasVariance || x.Status == StockTakeLineStatus.Accepted || x.Status == StockTakeLineStatus.RecountRequested),
            ReviewFilter.Unsettled => lines.Where(x => x.NeedsDecision || _frozenFilterIds.Contains(x.Id)),
            ReviewFilter.Accepted => lines.Where(x => x.Status == StockTakeLineStatus.Accepted),
            _ => lines
        };

        private int MatchCount => FilteredLines.Count();

        private int TotalPages => Math.Max(1, (int)Math.Ceiling((double)MatchCount / PageSize));

        private IEnumerable<StockTakeLineModel> PagedLines
            => FilteredLines.Skip((Math.Min(_page, TotalPages) - 1) * PageSize).Take(PageSize);

        private string RangeLabel
        {
            get
            {
                var total = MatchCount;
                if (total == 0) return "No parts";

                var first = ((Math.Min(_page, TotalPages) - 1) * PageSize) + 1;
                var last = Math.Min(first + PageSize - 1, total);

                return $"{first}–{last} of {total}";
            }
        }

        private void NextPage()
        {
            if (_page >= TotalPages) return;
            _page++;
        }

        private void PreviousPage()
        {
            if (_page <= 1) return;
            _page--;
        }

        /// <summary>
        /// Pins the rows the filter currently admits, so entering a figure does not make the row
        /// vanish. Re-taken whenever the filter itself changes.
        /// </summary>
        private void FreezeFilter()
        {
            _frozenFilterIds.Clear();

            if (_stockTake is null) return;

            if (_mode == ScreenMode.Count)
            {
                var pinned = _filter switch
                {
                    LineFilter.ToCount => _stockTake.Lines.Where(x => !x.IsCounted),
                    LineFilter.Counted => _stockTake.Lines.Where(x => x.IsCounted),
                    LineFilter.Recount => _stockTake.Lines.Where(x => x.Status == StockTakeLineStatus.RecountRequested),
                    _ => Enumerable.Empty<StockTakeLineModel>()
                };

                foreach (var line in pinned)
                    _frozenFilterIds.Add(line.Id);

                return;
            }

            if (_reviewFilter == ReviewFilter.Unsettled)
            {
                foreach (var line in _stockTake.Lines.Where(x => x.NeedsDecision))
                    _frozenFilterIds.Add(line.Id);
            }
        }

        private void SetMode(ScreenMode mode)
        {
            if (_mode == mode) return;

            _mode = mode;
            _page = 1;
            FreezeFilter();
        }

        private void SetFilter(LineFilter filter)
        {
            _filter = filter;
            _page = 1;
            FreezeFilter();
        }

        private void SetReviewFilter(ReviewFilter filter)
        {
            _reviewFilter = filter;
            _page = 1;
            FreezeFilter();
        }

        private async Task OnOrderChanged(ChangeEventArgs e)
        {
            if (!int.TryParse(e.Value?.ToString(), out var raw)) return;

            var order = (StockTakeSheetOrder)raw;
            if (order == _order) return;

            _order = order;
            _page = 1;

            // Re-read rather than re-sort in place, so the screen and the printed sheet are put in
            // order by the same code.
            await LoadStockTake();
        }

        // ---------------------------------------------------------------------
        // Capturing counts
        // ---------------------------------------------------------------------

        // Parsed by hand rather than bound, so a cleared box reads as "not counted" instead of
        // raising a binding error mid-count.
        private async Task OnQtyChanged(StockTakeLineModel line, string? raw)
        {
            int? quantity = null;

            if (!string.IsNullOrWhiteSpace(raw))
            {
                if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
                {
                    ToastService.Notify(new(ToastType.Danger, $"{line.PartCode}: enter a whole number of zero or more."));
                    return;
                }

                quantity = parsed;
            }

            line.Quantity = quantity;
            line.Status = quantity.HasValue ? StockTakeLineStatus.Counted : StockTakeLineStatus.NotCounted;

            await SaveLine(line);
        }

        /// <summary>
        /// The +/- buttons, which are what a count is actually entered with on a phone. From
        /// uncounted, "+" starts at one and "-" records an empty shelf: both are the single tap
        /// that a small count deserves.
        /// </summary>
        private async Task StepQty(StockTakeLineModel line, int direction)
        {
            var next = line.Quantity.HasValue
                ? Math.Max(0, line.Quantity.Value + direction)
                : direction > 0 ? 1 : 0;

            line.Quantity = next;
            line.Status = StockTakeLineStatus.Counted;

            await SaveLine(line);
        }

        private async Task ClearQty(StockTakeLineModel line)
        {
            line.Quantity = null;
            line.Status = StockTakeLineStatus.NotCounted;

            await SaveLine(line);
        }

        /// <summary>
        /// Written back as soon as a figure is entered rather than on a save button. A count is
        /// done walking round a warehouse on a phone, and the connection that drops mid-aisle
        /// should cost the line being typed, not the morning.
        /// </summary>
        private async Task SaveLine(StockTakeLineModel line)
        {
            if (_stockTake is null) return;

            try
            {
                await _stockTakeService.SaveCounts(
                    _stockTake.Id,
                    new[] { new StockTakeCountEntryModel { LineId = line.Id, Quantity = line.Quantity, Remarks = line.Remarks } },
                    _currentUser);

                if (_stockTake.Status == StockTakeStatus.Pending && _stockTake.Lines.Any(x => x.IsCounted))
                    _stockTake.Status = StockTakeStatus.Counting;

                _saveFailed = false;
                _saveState = $"Saved {DateTime.Now:HH:mm:ss}";
            }
            catch (InvalidOperationException ex)
            {
                _saveFailed = true;
                _saveState = "Not saved";
                ToastService.Notify(new(ToastType.Danger, ex.Message));
            }
            catch (Exception)
            {
                _saveFailed = true;
                _saveState = "Not saved";
                ToastService.Notify(new(ToastType.Danger, $"{line.PartCode} could not be saved. Check your connection and try again."));
            }
        }

        // ---------------------------------------------------------------------
        // Review and posting
        // ---------------------------------------------------------------------

        private async Task FinishCounting()
        {
            if (_stockTake is null) return;

            if (_stockTake.NotCountedCount > 0)
            {
                var proceed = await _confirmationService.ConfirmAsync(
                    $"{_stockTake.NotCountedCount} part(s) have not been counted. They will be left exactly as they are, " +
                    "not treated as zero. Move on to the differences?");

                if (!proceed) return;
            }

            try
            {
                await _stockTakeService.StartReview(_stockTake.Id, _currentUser);
                await LoadStockTake();
                _mode = ScreenMode.Review;
                _reviewFilter = ReviewFilter.Unsettled;
                _page = 1;
                FreezeFilter();
            }
            catch (InvalidOperationException ex)
            {
                ToastService.Notify(new(ToastType.Danger, ex.Message));
            }
        }

        private async Task ReopenCounting()
        {
            if (_stockTake is null) return;

            try
            {
                await _stockTakeService.ReopenCounting(_stockTake.Id, _currentUser);
                await LoadStockTake();
                _mode = ScreenMode.Count;
                _filter = LineFilter.ToCount;
                _page = 1;
                FreezeFilter();
            }
            catch (InvalidOperationException ex)
            {
                ToastService.Notify(new(ToastType.Danger, ex.Message));
            }
        }

        private async Task AcceptLine(StockTakeLineModel line)
        {
            if (_stockTake is null) return;

            try
            {
                await _stockTakeService.AcceptLines(_stockTake.Id, new[] { line.Id }, _currentUser);

                line.FinalQuantity = line.Quantity;
                line.Status = StockTakeLineStatus.Accepted;
            }
            catch (InvalidOperationException ex)
            {
                ToastService.Notify(new(ToastType.Danger, ex.Message));
            }
        }

        private async Task AcceptAll()
        {
            if (_stockTake is null) return;

            var pending = _stockTake.Lines.Where(x => x.NeedsDecision).ToList();
            if (pending.Count == 0) return;

            var value = pending.Sum(x => x.VarianceValue).ToString("C", CultureInfo.CurrentCulture);

            var confirmed = await _confirmationService.ConfirmAsync(
                $"Accept all {pending.Count} remaining difference(s)? That is a value impact of {value}.");

            if (!confirmed) return;

            _loaderService.ShowLoader();
            try
            {
                await _stockTakeService.AcceptLines(_stockTake.Id, pending.Select(x => x.Id), _currentUser);

                foreach (var line in pending)
                {
                    line.FinalQuantity = line.Quantity;
                    line.Status = StockTakeLineStatus.Accepted;
                }
            }
            catch (InvalidOperationException ex)
            {
                ToastService.Notify(new(ToastType.Danger, ex.Message));
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }

        private async Task RecountLine(StockTakeLineModel line)
        {
            if (_stockTake is null) return;

            try
            {
                await _stockTakeService.RequestRecount(_stockTake.Id, new[] { line.Id }, _currentUser);

                line.PreviousQuantity = line.Quantity ?? line.PreviousQuantity;
                line.Quantity = null;
                line.FinalQuantity = null;
                line.Status = StockTakeLineStatus.RecountRequested;
                line.RecountCount++;

                // Asking for a recount is asking for counting to happen, so the sheet says so.
                _stockTake.Status = StockTakeStatus.Counting;
            }
            catch (InvalidOperationException ex)
            {
                ToastService.Notify(new(ToastType.Danger, ex.Message));
            }
        }

        private async Task PostStockTake()
        {
            if (_stockTake is null) return;

            var value = _stockTake.TotalVarianceValue.ToString("C", CultureInfo.CurrentCulture);
            var net = _stockTake.NetUnitAdjustment;

            var message = $"Post {_stockTake.Reference}? Stock will be corrected on "
                        + $"{_stockTake.Lines.Count(x => x.PendingDelta != 0)} part(s), a net of "
                        + $"{(net > 0 ? "+" : "")}{net} units and a value impact of {value}.";

            if (_stockTake.NotCountedCount > 0)
                message += $" {_stockTake.NotCountedCount} uncounted part(s) will be left unchanged.";

            var confirmed = await _confirmationService.ConfirmAsync(message);
            if (!confirmed) return;

            _loaderService.ShowLoader();
            try
            {
                var result = await _stockTakeService.PostStockTake(_stockTake.Id, _currentUser);

                ToastService.Notify(new(ToastType.Success,
                    $"{result.Reference} posted: {result.LinesAdjusted} part(s) corrected, "
                    + $"+{result.UnitsAdded} / -{result.UnitsRemoved} units, "
                    + $"{result.ValueAdjustment.ToString("C", CultureInfo.CurrentCulture)}."));

                if (result.LinesMovedDuringCount > 0)
                {
                    ToastService.Notify(new(ToastType.Warning,
                        $"{result.LinesMovedDuringCount} part(s) had moved since the sheet was raised. "
                        + "Their corrections were applied as differences, so those movements were kept."));
                }

                await LoadStockTake();
            }
            catch (InvalidOperationException ex)
            {
                ToastService.Notify(new(ToastType.Danger, ex.Message));
            }
            catch (Exception)
            {
                ToastService.Notify(new(ToastType.Danger, "The stock take could not be posted. No stock was changed."));
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }

        // ---------------------------------------------------------------------
        // Presentation helpers
        // ---------------------------------------------------------------------

        /// <summary>
        /// When the sheet was posted and by whom, built as one sentence so an unknown user leaves
        /// no gap in it. Shown in local time: unlike the dates elsewhere in the app this carries a
        /// time of day, and a time of day is read against the clock on the wall.
        /// </summary>
        private string PostedLabel
        {
            get
            {
                if (_stockTake?.CompletedDate is null) return "Posted.";

                var when = _stockTake.CompletedDate.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

                return string.IsNullOrWhiteSpace(_stockTake.CompletedBy)
                    ? $"Posted {when}."
                    : $"Posted {when} by {_stockTake.CompletedBy}.";
            }
        }

        private static string? RowStateClass(StockTakeLineModel line) => line.Status switch
        {
            StockTakeLineStatus.RecountRequested => "st-row-recount",
            StockTakeLineStatus.Accepted => "st-row-counted",
            StockTakeLineStatus.Counted => "st-row-counted",
            _ => null
        };

        private static string? ReviewRowStateClass(StockTakeLineModel line)
        {
            if (line.Status == StockTakeLineStatus.RecountRequested) return "st-row-recount";
            if (line.NeedsDecision) return "st-row-unsettled";
            if (line.Status == StockTakeLineStatus.Accepted) return "st-row-accepted";
            return null;
        }

        private static string? VarianceClass(StockTakeLineModel line)
        {
            if (!line.IsCounted || line.Variance == 0) return null;
            return line.Variance < 0 ? "st-value-down" : "st-value-up";
        }

        private void BackToList() => _navigationManager.NavigateTo("stock-takes");

        private async Task DownloadCountSheet()
        {
            if (_stockTake is null) return;

            _loaderService.ShowLoader();
            try
            {
                var pdf = await _stockTakeService.GenerateCountSheet(_stockTake.Id, _order);
                await DownloadFileFromStream(pdf, $"CountSheet_{_stockTake.Reference}.pdf");
            }
            catch (Exception)
            {
                ToastService.Notify(new(ToastType.Danger, "The count sheet could not be generated."));
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }

        private async Task DownloadFileFromStream(byte[] fileBytes, string fileName)
        {
            var fileStream = new MemoryStream(fileBytes);
            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
    }
}
