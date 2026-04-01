using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA.Core
{
    public enum ScrollDirection
    {
        Vertical,
        Horizontal
    }
    public enum LayoutType
    {
        List,
        Grid
    }
    public enum ChildAlignment
    {
        UpperLeft, UpperCenter, UpperRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        LowerLeft, LowerCenter, LowerRight
    }


    [RequireComponent(typeof(ScrollRect))]
    public class RecycleScrollView : MonoBehaviour
    {
        [Header("Cell")]
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private Vector2 cellSize = new Vector2(100, 100);
        [SerializeField] private Vector2 spacing = new Vector2(10, 10);
        [SerializeField] private RectOffset padding = new RectOffset();

        [Header("Layout")]
        [SerializeField] private ScrollDirection scrollDirection = ScrollDirection.Vertical;
        [SerializeField] private LayoutType layoutType = LayoutType.List;
        [SerializeField] private int gridColumns = 1;
        [SerializeField] private int gridRows = 1;
        [SerializeField] private ChildAlignment childAlignment = ChildAlignment.UpperLeft;

        [Header("셀 크기 자동 맞춤")]
        [Tooltip("true면 Cross축 셀 크기를 뷰포트에 맞춰 자동 계산")]
        [SerializeField] private bool autoFitCrossAxis = false;

        [Header("고정 셀 개수 (0이면 자동 계산)")]
        [SerializeField] private int fixedCellCount = 0;

        [Header("여분 셀 개수")]
        [SerializeField] private int bufferCount = 1;

        [Header("SiblingOrder")]
        [SerializeField] private bool updateSiblingOrder = false;

        [Header("그리드 혼합 배치")]
        [SerializeField] private int columnMajorFromBlockIndex = 0;

        private ScrollRect _scrollRect;
        private RectTransform _viewport;
        private RectTransform _content;
        private readonly List<RectTransform> _cells = new List<RectTransform>();
        private int[] _cellDataIndices;
        
        private int _totalCount;
        private int _cellCount;
        private int _headIndex;
        private int _tailIndex;
        private bool _isInitialized;


        public Action<GameObject, int> OnCellUpdate { get; set; }
        public int TotalCount => _totalCount;
        
        public GameObject CellPrefab
        {
            get => cellPrefab;
            set => cellPrefab = value;
        }
        public Vector2 CellSize
        {
            get => cellSize;
            set => cellSize = value;
        }
        public Vector2 Spacing
        {
            get => spacing;
            set => spacing = value;
        }
        public int GridColumns
        {
            get => gridColumns;
            set => gridColumns = Mathf.Max(1, value);
        }
        public int GridRows
        {
            get => gridRows;
            set => gridRows = Mathf.Max(1, value);
        }
        public bool AutoFitCrossAxis
        {
            get => autoFitCrossAxis;
            set => autoFitCrossAxis = value;
        }
        public int FixedCellCount
        {
            get => fixedCellCount;
            set => fixedCellCount = Mathf.Max(0, value);
        }
        public bool UpdateSiblingOrder
        {
            get => updateSiblingOrder;
            set => updateSiblingOrder = value;
        }

        public int ColumnMajorFromBlockIndex
        {
            get => columnMajorFromBlockIndex;
            set => columnMajorFromBlockIndex = Mathf.Max(0, value);
        }
        
        private bool IsVertical => scrollDirection == ScrollDirection.Vertical;
        private int Cols => Mathf.Max(1, gridColumns);
        private int Rows => Mathf.Max(1, gridRows);

        // 한 블록(gridColumns * gridRows)의 아이템 수
        private int ItemsPerBlock => layoutType == LayoutType.List ? 1 : Cols * Rows;

        // Cross축 아이템 수 (Vertical 열 수, Horizontal 행 수)
        private int CrossAxisCount => layoutType == LayoutType.List ? 1 : IsVertical ? Cols : Rows;

        // Main축으로 한 블록이 차지하는 셀 수
        private int MainAxisCellsPerBlock => layoutType == LayoutType.List ? 1 : IsVertical ? Rows : Cols;

        private float MainAxisBlockStride => MainAxisCellsPerBlock * (IsVertical ? cellSize.y + spacing.y : cellSize.x + spacing.x);

        private int BlockCount => _totalCount == 0 ? 0 : Mathf.CeilToInt((float)_totalCount / ItemsPerBlock);

        private void Awake() => Initialize();

        private void OnDestroy()
        {
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }

        public void SetItemCount(int count)
        {
            Initialize();
            _totalCount = count;
            
            SetupScrollRect();
            ApplyAutoFitCrossAxis();
            UpdateContentSize();
            EnsureCells();
            SyncAllCellSizes();
            InitializeCellPositions();

            if (_hasSavedScrollPosition)
                TryRestoreSavedScrollPosition();
            else if (_content != null)
                _content.anchoredPosition = Vector2.zero;
        }

        public void RefreshAllCells()
        {
            for (int i = 0; i < _cellCount; i++)
            {
                int dataIndex = _cellDataIndices[i];
                if (dataIndex >= 0 && dataIndex < _totalCount)
                    OnCellUpdate?.Invoke(_cells[i].gameObject, dataIndex);
            }
        }

        public void RefreshCell(int dataIndex)
        {
            int cellIndex = FindCellIndexByDataIndex(dataIndex);
            if (cellIndex >= 0)
                OnCellUpdate?.Invoke(_cells[cellIndex].gameObject, dataIndex);
        }

        public void ScrollToIndex(int index, bool animated = false)
        {
            _hasSavedScrollPosition = false;

            if (_totalCount == 0)
                return;
            
            index = Mathf.Clamp(index, 0, _totalCount - 1);
            var position = GetCellPosition(index);
            
            if (IsVertical)
            {
                float maxScroll = Mathf.Max(0, _content.rect.height - _viewport.rect.height);
                float targetY = Mathf.Clamp(-position.y - padding.top, 0, maxScroll);
                _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, targetY);
            }
            else
            {
                float maxScroll = Mathf.Max(0, _content.rect.width - _viewport.rect.width);
                float targetX = Mathf.Clamp(position.x - padding.left, 0, maxScroll);
                _content.anchoredPosition = new Vector2(-targetX, _content.anchoredPosition.y);
            }
            
            UpdateCellPositions();
        }

        public void ScrollToTop() => ScrollToIndex(0);
        public void ScrollToBottom() => ScrollToIndex(_totalCount - 1);
        public GameObject GetCellByIndex(int dataIndex)
        {
            int cellIndex = FindCellIndexByDataIndex(dataIndex);
            return cellIndex >= 0 ? _cells[cellIndex].gameObject : null;
        }
        public int GetCellIndex(GameObject cell)
        {
            var rt = cell.GetComponent<RectTransform>();
            int cellIndex = _cells.IndexOf(rt);
            return cellIndex >= 0 ? _cellDataIndices[cellIndex] : -1;
        }

        private void Initialize()
        {
            if (_isInitialized)
                return;
            
            _scrollRect = GetComponent<ScrollRect>();
            _viewport = _scrollRect.viewport;
            _content = _scrollRect.content;
            
            if (cellPrefab != null)
                cellPrefab.SetActive(false);
            
            SetupScrollRect();
            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            _isInitialized = true;
        }

        private void SetupScrollRect()
        {
            if (_content == null)
                return;

            var layoutGroup = _content.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
                layoutGroup.enabled = false;
            
            var sizeFitter = _content.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null)
                sizeFitter.enabled = false;

            _scrollRect.horizontal = !IsVertical;
            _scrollRect.vertical = IsVertical;

            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(0, 1);
            _content.pivot = new Vector2(0, 1);
        }
        
        private void EnsureCells()
        {
            int requiredCount = CalculateRequiredCellCount();
            requiredCount = Mathf.Min(requiredCount, _totalCount);

            while (_cells.Count < requiredCount)
                CreateCell();
            
            _cellCount = Mathf.Min(_cells.Count, requiredCount);
            _cellDataIndices = new int[_cellCount];
        }

        private int CalculateRequiredCellCount()
        {
            if (fixedCellCount > 0)
                return fixedCellCount + (bufferCount * 2 * ItemsPerBlock);
            
            float viewportSize = IsVertical ? _viewport.rect.height : _viewport.rect.width;
            float stride = MainAxisBlockStride;
            
            int visibleBlocks = Mathf.CeilToInt(viewportSize / stride) + 1;
            return (visibleBlocks + bufferCount * 2) * ItemsPerBlock;
        }

        private void CreateCell()
        {
            var cellGO = Instantiate(cellPrefab, _content);
            cellGO.SetActive(true);
            var rt = cellGO.GetComponent<RectTransform>();

            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = cellSize;
            
            _cells.Add(rt);
        }

        private void InitializeCellPositions()
        {
            foreach (var cell in _cells)
                cell.gameObject.SetActive(false);
            
            if (_totalCount == 0 || _cellCount == 0)
                return;
            
            _headIndex = 0;
            _tailIndex = Mathf.Min(_cellCount - 1, _totalCount - 1);

            for (int i = 0; i < _cellCount && i < _totalCount; i++)
            {
                _cellDataIndices[i] = i;
                _cells[i].anchoredPosition = GetCellPosition(i);
                _cells[i].gameObject.SetActive(true);
                OnCellUpdate?.Invoke(_cells[i].gameObject, i);
            }
        }

        private void ApplyAutoFitCrossAxis()
        {
            if (!autoFitCrossAxis || layoutType == LayoutType.List || _viewport == null)
                return;

            int count = CrossAxisCount;
            if (IsVertical)
            {
                float available = _viewport.rect.width - padding.left - padding.right - (count - 1) * spacing.x;
                cellSize.x = Mathf.Max(1f, available / count);
            }
            else
            {
                float available = _viewport.rect.height - padding.top - padding.bottom - (count - 1) * spacing.y;
                cellSize.y = Mathf.Max(1f, available / count);
            }
        }

        private void SyncAllCellSizes()
        {
            foreach (var cell in _cells)
                cell.sizeDelta = cellSize;
        }

        private void UpdateContentSize()
        {
            if (_totalCount == 0)
            {
                _content.sizeDelta = Vector2.zero;
                return;
            }

            int mainAxisCells = CalculateMainAxisCells();
            int crossAxisCells = CrossAxisCount;
            
            float mainSize = IsVertical
                ? padding.top + padding.bottom + mainAxisCells * cellSize.y + Mathf.Max(0, mainAxisCells - 1) * spacing.y
                : padding.left + padding.right + mainAxisCells * cellSize.x + Mathf.Max(0, mainAxisCells - 1) * spacing.x;
            
            float crossSize = IsVertical
                ? padding.left + padding.right + crossAxisCells * cellSize.x + Mathf.Max(0, crossAxisCells - 1) * spacing.x
                : padding.top + padding.bottom + crossAxisCells * cellSize.y + Mathf.Max(0, crossAxisCells - 1) * spacing.y;

            _content.sizeDelta = IsVertical ? new Vector2(crossSize, mainSize) : new Vector2(mainSize, crossSize);
        }

        private int CalculateMainAxisCells()
        {
            if (layoutType == LayoutType.List)
                return _totalCount;

            int cols = Cols;
            int rows = Rows;
            int blockSize = cols * rows;
            int fullBlocks = _totalCount / blockSize;
            int remaining = _totalCount % blockSize;

            int mainPerBlock = IsVertical ? rows : cols;
            int fullCells = fullBlocks * mainPerBlock;

            if (remaining == 0)
                return fullCells;

            int lastMainCells;
            if (columnMajorFromBlockIndex <= 0)
            {
                lastMainCells = IsVertical
                    ? Mathf.Min(rows, Mathf.CeilToInt((float)remaining / cols))
                    : Mathf.Min(cols, Mathf.CeilToInt((float)remaining / rows));
            }
            else
            {
                int lastBlockIndex = fullBlocks;
                lastMainCells = IsVertical
                    ? Mathf.Min(rows, ComputePartialBlockMainSpanRows(remaining, lastBlockIndex, cols, rows))
                    : Mathf.Min(cols, ComputePartialBlockMainSpanCols(remaining, lastBlockIndex, cols, rows));
            }
            return fullCells + lastMainCells;
        }

        private int ComputePartialBlockMainSpanRows(int remaining, int blockIndex, int cols, int rows)
        {
            int maxR = 0;
            for (int i = 0; i < remaining; i++)
            {
                GetBlockCellRowCol(blockIndex, i, cols, rows, out int r, out _);
                if (r > maxR) maxR = r;
            }
            return maxR + 1;
        }

        private int ComputePartialBlockMainSpanCols(int remaining, int blockIndex, int cols, int rows)
        {
            int maxC = 0;
            for (int i = 0; i < remaining; i++)
            {
                GetBlockCellRowCol(blockIndex, i, cols, rows, out _, out int c);
                if (c > maxC) maxC = c;
            }
            return maxC + 1;
        }

        private void GetBlockCellRowCol(int block, int inBlock, int cols, int rows, out int r, out int c)
        {
            if (columnMajorFromBlockIndex <= 0 || block < columnMajorFromBlockIndex)
            {
                r = inBlock / cols;
                c = inBlock % cols;
                return;
            }

            r = inBlock % rows;
            c = inBlock / rows;
        }

        private Vector2 GetCellPosition(int index)
        {
            if (layoutType == LayoutType.List)
            {
                if (IsVertical)
                    return new Vector2(padding.left, -(padding.top + index * (cellSize.y + spacing.y)));
                else
                    return new Vector2(padding.left + index * (cellSize.x + spacing.x), -padding.top);
            }

            int cols = Cols;
            int rows = Rows;
            int blockSize = cols * rows;

            int block = index / blockSize;
            int inBlock = index % blockSize;

            GetBlockCellRowCol(block, inBlock, cols, rows, out int r, out int c);

            if (IsVertical)
            {
                float x = padding.left + GetHorizontalAlignmentOffset() + c * (cellSize.x + spacing.x);
                float y = -(padding.top + (block * rows + r) * (cellSize.y + spacing.y));
                return new Vector2(x, y);
            }
            else
            {
                float x = padding.left + (block * cols + c) * (cellSize.x + spacing.x);
                float y = -(padding.top + GetVerticalAlignmentOffset() + r * (cellSize.y + spacing.y));
                return new Vector2(x, y);
            }
        }

        private float GetHorizontalAlignmentOffset()
        {
            int horizontal = (int)childAlignment % 3;
            if (horizontal == 0)
                return 0f;
            
            int count = CrossAxisCount;
            float totalWidth = count * cellSize.x + (count - 1) * spacing.x;
            float available = _viewport.rect.width - padding.left - padding.right;
            float remaining = available - totalWidth;
            
            if (remaining <= 0)
                return 0f;
            return horizontal == 1 ? remaining / 2f : remaining;
        }

        private float GetVerticalAlignmentOffset()
        {
            int vertical = (int)childAlignment / 3;
            if (vertical == 0)
                return 0f;
            
            int count = CrossAxisCount;
            float totalHeight = count * cellSize.y + (count - 1) * spacing.y;
            float available = _viewport.rect.height - padding.top - padding.bottom;
            float remaining = available - totalHeight;
            
            if (remaining <= 0)
                return 0f;
            return vertical == 1 ? remaining / 2f : remaining;
        }

        private void OnScrollValueChanged(Vector2 _) => UpdateCellPositions();

        private void UpdateCellPositions()
        {
            if (_totalCount == 0 || _cellCount == 0 || _cellCount >= _totalCount)
                return;

            float stride = MainAxisBlockStride;
            float paddingStart = IsVertical ? padding.top : padding.left;
            float scrollOffset = IsVertical ? _content.anchoredPosition.y : -_content.anchoredPosition.x;

            int firstVisibleBlock = scrollOffset > paddingStart ? Mathf.FloorToInt((scrollOffset - paddingStart) / stride) : 0;

            int targetStartBlock = Mathf.Max(0, firstVisibleBlock - bufferCount);
            int targetStartIndex = Mathf.Min(targetStartBlock * ItemsPerBlock, Mathf.Max(0, _totalCount - _cellCount));

            while (_headIndex < targetStartIndex && _tailIndex < _totalCount - 1)
            {
                int cellIndex = FindCellIndexByDataIndex(_headIndex);
                if (cellIndex < 0)
                    break;

                _tailIndex++;
                UpdateCell(cellIndex, _tailIndex, true);
                _headIndex++;
            }

            while (_headIndex > targetStartIndex && _headIndex > 0)
            {
                int cellIndex = FindCellIndexByDataIndex(_tailIndex);
                if (cellIndex < 0)
                    break;

                _headIndex--;
                UpdateCell(cellIndex, _headIndex, false);
                _tailIndex--;
            }
        }

        private void UpdateCell(int cellIndex, int dataIndex, bool toEnd)
        {
            _cellDataIndices[cellIndex] = dataIndex;
            _cells[cellIndex].anchoredPosition = GetCellPosition(dataIndex);
            
            if (updateSiblingOrder)
            {
                if (toEnd)
                    _cells[cellIndex].SetAsLastSibling();
                else
                    _cells[cellIndex].SetAsFirstSibling();
            }
            
            OnCellUpdate?.Invoke(_cells[cellIndex].gameObject, dataIndex);
        }

        private Vector2 _savedScrollPosition;
        private bool _hasSavedScrollPosition;

        public void SaveScrollPosition()
        {
            if (_content != null)
            {
                _savedScrollPosition = _content.anchoredPosition;
                _hasSavedScrollPosition = true;
            }
        }

        public void SaveScrollPosition(Vector2 position)
        {
            _savedScrollPosition = position;
            _hasSavedScrollPosition = true;
        }

        private void TryRestoreSavedScrollPosition()
        {
            if (!_hasSavedScrollPosition || _content == null || _totalCount == 0)
                return;
            _hasSavedScrollPosition = false;
            _content.anchoredPosition = _savedScrollPosition;
            UpdateCellPositions();
        }

        private int FindCellIndexByDataIndex(int dataIndex)
        {
            for (int i = 0; i < _cellCount; i++)
            {
                if (_cellDataIndices[i] == dataIndex)
                    return i;
            }
            return -1;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            gridColumns = Mathf.Max(1, gridColumns);
            gridRows = Mathf.Max(1, gridRows);
            bufferCount = Mathf.Max(0, bufferCount);
            cellSize = new Vector2(Mathf.Max(1, cellSize.x), Mathf.Max(1, cellSize.y));
            columnMajorFromBlockIndex = Mathf.Max(0, columnMajorFromBlockIndex);
        }
#endif
    }
}
