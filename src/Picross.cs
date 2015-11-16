using System;
using System.Collections.Generic;
using System.Linq;

namespace PicrossSolver
{
    public enum SolutionState
    {
        Empty, True, False
    }


    public class Puzzle
    {
        //===================================================================== VARIABLES
        private readonly List<int[]> _cluesX = new List<int[]>();
        private readonly List<int[]> _cluesY = new List<int[]>();
        private SolutionState[,] _solution;

        //===================================================================== INITIALIZE
        public Puzzle(string rawText)
        {
            LoadPuzzle(rawText);
        }
        private void LoadPuzzle(string rawText)
        {
            string[] rawX = rawText.Split('-')[0].Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            string[] rawY = rawText.Split('-')[1].Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string x in rawX)
            {
                string[] split = x.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                _cluesX.Add(new int[split.Length]);
                for (int i = 0; i < split.Length; i++) _cluesX[_cluesX.Count - 1][i] = Convert.ToInt32(split[i]);
            }
            foreach (string y in rawY)
            {
                string[] split = y.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                _cluesY.Add(new int[split.Length]);
                for (int i = 0; i < split.Length; i++) _cluesY[_cluesY.Count - 1][i] = Convert.ToInt32(split[i]);
            }
            _solution = new SolutionState[_cluesX.Count, _cluesY.Count];
        }

        //===================================================================== FUNCTION
        public void Solve()
        {
            // store number of changes
            int changes = 0;
            // iterate through grid
            do
            {
                changes = 0;
                foreach (bool isX in new bool[] { true, false })
                {
                    int dimensionLength = (isX ? Height : Width);
                    for (int lineID = 0; lineID < dimensionLength; lineID++)
                    {
                        int[] clue = GetClue(lineID, isX);
                        SolutionLine line = ReadSolutionLine(lineID, isX);
                        SolutionState[] items = new SolutionState[line.Items.Length];
                        Array.Copy(line.Items, items, line.Items.Length);
                        line.Solve();
                        for (int i = 0; i < items.Length; i++)
                        {
                            if (items[i] != line.Items[i]) changes++;
                        }
                        WriteSolutionLine(line);
                    }
                }
                //Console.WriteLine(changes);
            }
            while (changes > 0);
        }
        public int[] GetClue(int clueID, bool isX)
        {
            return (isX ? _cluesX[clueID] : _cluesY[clueID]);
        }
        public SolutionLine ReadSolutionLine(int lineID, bool isX)
        {
            int[] clue;
            SolutionState[] line;
            if (isX)
            {
                clue = _cluesX[lineID];
                line = new SolutionState[Height];
                for (int y = 0; y < Height; y++) line[y] = _solution[lineID, y];
            }
            else
            {
                clue = _cluesY[lineID];
                line = new SolutionState[Width];
                for (int x = 0; x < Width; x++) line[x] = _solution[x, lineID];
            }
            return new SolutionLine(clue, line, lineID, isX);
        }
        public void WriteSolutionLine(SolutionLine line)
        {
            if (line.IsX)
                for (int y = 0; y < line.Length; y++) _solution[line.LineID, y] = line.Items[y];
            else
                for (int x = 0; x < line.Length; x++) _solution[x, line.LineID] = line.Items[x];
        }
        public SolutionState GetSolution(int x, int y)
        {
            return _solution[x, y];
        }

        //===================================================================== PROPERTIES
        public int Width
        {
            get { return _cluesX.Count; }
        }
        public int Height
        {
            get { return _cluesY.Count; }
        }
        public int MaxClueCountX
        {
            get
            {
                int maxCount = 0;
                foreach (int[] clue in _cluesX) maxCount = Math.Max(maxCount, clue.Length);
                return maxCount;
            }
        }
        public int MaxClueCountY
        {
            get
            {
                int maxCount = 0;
                foreach (int[] clue in _cluesY) maxCount = Math.Max(maxCount, clue.Length);
                return maxCount;
            }
        }
    }


    public class SolutionLine
    {
        //===================================================================== VARIABLES
        public readonly int LineID;
        public readonly bool IsX;
        public readonly int[] Clue;
        public SolutionState[] Items;

        //===================================================================== INITIALIZE
        public SolutionLine(int[] clue, SolutionState[] line, int lineID, bool isX)
        {
            Clue = clue;
            Items = line;
            LineID = lineID;
            IsX = isX;
        }

        //===================================================================== FUNCTION
        public void Solve()
        {
            if (!IsSolved)
            {
                SolveSimpleBoxes(); // 1 clue
                // SolveSubLine();
                SolveUnsolvedEdge();
                SolveUnsolvedEdge2();
                SolveAlreadyCompleted(); // any number
                SolveGlue(); // any number
                SolveContradiction(); // any number
                SolveFalses(); // any number
            }
        }
        private void SolveSimpleBoxes()
        {
            //if (LineID == 1 && IsX) Console.WriteLine(IsX + ": " + LineID + ": " + clueID + ": " + boundary[0] + ", " + boundary[1]);
            if (IsSolved) return;
            for (int clueID = 0; clueID < Clue.Length; clueID++)
            {
                int clue = Clue[clueID];
                int[] boundary = GetPotentialClueBoundary(clueID);
                int length = boundary[1] - boundary[0] + 1;
                if (length < clue * 2 - 1 + length % 2) ModifyState(SolutionState.True, boundary[0] + length - clue, clue - (length - clue));
            }
        }
        private void SolveUnsolvedEdge()
        {
            int solvedClueCount = GetSolvedClueCountFromStart();
            if (solvedClueCount != 0 && solvedClueCount != Clue.Length)
            {
                int[] clue = Clue.Skip(solvedClueCount).ToArray();
                int start = UnsolvedStart;
                SplitAndSolve(clue, start, UpperBound);
            }
            solvedClueCount = GetSolvedClueCountFromEnd();
            if (solvedClueCount != 0 && solvedClueCount != Clue.Length)
            {
                int[] clue = Clue.Take(Clue.Length - solvedClueCount).ToArray();
                int end = UnsolvedEnd;
                SplitAndSolve(clue, LowerBound, end);
            }
        } // filters out already solved edges, eg. XXOOXXOX-, starts at -
        private void SolveUnsolvedEdge2()
        {
            if (Clue.Length != 1 && !IsSolved)
            {
                int start = UnsolvedStart;
                if (Items[start] != SolutionState.False && DoesSpaceContainTrueFromID(start))
                {
                    int clueID = GetSolvedClueCountFromStart();
                    int length = GetSpaceSizeFromID(start);
                    if (clueID < Clue.Length - 1 && length < Clue[clueID] + Clue[clueID + 1] + 1)
                    {
                        SplitAndSolve(new int[] { Clue[clueID] }, start, start + length - 1);
                        SplitAndSolve(Clue.Skip(clueID + 1).ToArray(), start + length, UpperBound);
                    }
                }
                int end = UnsolvedEnd;
                if (Items[end] != SolutionState.False && DoesSpaceContainTrueFromID(end))
                {
                    int clueID = Clue.Length - GetSolvedClueCountFromEnd() - 1;
                    int length = GetSpaceSizeFromID(end);
                    if (clueID > 0 && length < Clue[clueID] + Clue[clueID - 1] + 1)
                    {
                        SplitAndSolve(new int[] { Clue[clueID] }, end - length + 1, end);
                        SplitAndSolve(Clue.Take(clueID).ToArray(), LowerBound, end - length);
                    }
                }
            }
        }
        public void SolveAlreadyCompleted()
        {
            if (Clue.Sum() == GetStateCount(SolutionState.Empty) + GetStateCount(SolutionState.True))
            {
                for (int i = EffectiveStart; i < Length; i++) if (Items[i] == SolutionState.Empty) ModifyState(SolutionState.True, i, 1);
            }
        }
        public void SolveGlue()
        {
            int start = EffectiveStart, end = EffectiveEnd;
            // if solved at edges
            if (FirstTrue != -1 && FirstTrue < start + FirstClue - 1)
                ModifyState(SolutionState.True, FirstTrue, FirstClue - (FirstTrue - start));
            if (LastTrue > end - LastClue + 1)
                ModifyState(SolutionState.True, end - LastClue + 1, LastClue - (end - LastTrue));
        }
        private void SolveContradiction()
        {
            for (int i = LowerBound; i < Length; i++)
            {
                if (Items[i] == SolutionState.Empty)
                {
                    ModifyState(SolutionState.True, i, 1);
                    if (LargestBlock > Clue.Max()) ModifyState(SolutionState.False, i, 1);
                    else ModifyState(SolutionState.Empty, i, 1);
                }
            }
        }
        private void SolveFalses()
        {
            // if line already solved, fill falses
            if (GetStateCount(SolutionState.True) == Clue.Sum())
            {
                for (int i = LowerBound; i < Length; i++)
                    if (Items[i] == SolutionState.Empty) ModifyState(SolutionState.False, i, 1);
            }
            // fill falses at impossible pts
            if (Clue.Length == 1)
            {
                int blockSize = LastTrue - FirstTrue + 1;
                if (FirstTrue != -1 && blockSize > 0)
                {
                    int leftStart = EffectiveStart, leftEnd = FirstTrue - (FirstClue - blockSize) - 1;
                    int rightStart = LastTrue + (FirstClue - blockSize) + 1, rightEnd = EffectiveEnd;
                    ModifyState(SolutionState.False, leftStart, leftEnd - leftStart + 1);
                    ModifyState(SolutionState.False, rightStart, rightEnd - rightStart + 1);
                }
            }
            // fill false at solved edges
            if (!IsSolved) // in the event block size = length
            {
                if (GetGroupSizeFromID(LowerBound, SolutionState.True) == FirstClue) ModifyState(SolutionState.False, FirstClue, 1);
                if (GetGroupSizeFromID(UpperBound, SolutionState.True) == LastClue) ModifyState(SolutionState.False, UpperBound - LastClue, 1);
            }
            // fill edge if too big for first clue
            int groupSize = GetGroupSizeFromID(LowerBound, SolutionState.Empty);
            if (groupSize < FirstClue && Items[groupSize] == SolutionState.False) ModifyState(SolutionState.False, LowerBound, groupSize);
            groupSize = GetGroupSizeFromID(UpperBound, SolutionState.Empty);
            if (groupSize < LastClue && Items[UpperBound - groupSize] == SolutionState.False) ModifyState(SolutionState.False, Length - groupSize, groupSize);
            // fill spaces to small for any clue
            for (int id = LowerBound; id < Length; id++)
            {
                int emptySize = GetGroupSizeFromID(id, SolutionState.Empty);
                if (emptySize < Clue.Min())
                {
                    if (IsGroupAtID(id, SolutionState.Empty, SolutionState.False))
                        ModifyState(SolutionState.False, id, emptySize);
                }
                if (id == EffectiveEnd)
                {
                    if (IsGroupAtID(id, SolutionState.Empty, SolutionState.False) && emptySize < LastClue)
                        ModifyState(SolutionState.False, id, emptySize);
                }
            }
        }

        private void ModifyState(SolutionState state, int start, int count)
        {
            try
            {
                for (int cnt = 0; cnt < count; cnt++) Items[start + cnt] = state;
            }
            catch (Exception e)
            {
                Console.WriteLine(new System.Diagnostics.StackFrame(1).GetMethod().Name + ": " + state.ToString() + ", " + start + ", " + count);
            }
        }
        private void SplitAndSolve(int[] clue, int start, int end)
        {
            SolutionLine subLine = ReadSolutionLine(clue, start, end);
            subLine.Solve();
            WriteSolutionLine(subLine);
        }
        private SolutionLine ReadSolutionLine(int[] clue, int start, int end)
        {
            int length = end - start + 1;
            SolutionState[] subLine = new SolutionState[length];
            for (int i = start; i <= end; i++) subLine[i - start] = Items[i];
            return new SolutionLine(clue, subLine, start, IsX);
        }
        private void WriteSolutionLine(SolutionLine line)
        {
            for (int i = line.LowerBound; i < line.Length; i++) Items[line.LineID + i] = line.Items[i];
        }
        private int GetStateCount(SolutionState state)
        {
            return Items.Count(isTrue => isTrue == state);
        }
        private int GetStateCount(SolutionState state, int start, int end)
        {
            int count = 0;
            for (int i = start; i <= end; i++) if (Items[i] == state) count++;
            return count;
        }
        private int[] GetPotentialClueBoundary(int clueID)
        {
            int start = EffectiveStart;
            int end = EffectiveEnd;
            for (int clueLeft = 0; clueLeft < clueID; clueLeft++) // iterate from left
            {
                start += Clue[clueLeft];
                if (Items[start] == SolutionState.True) start = GetGroupEndFromID(start);
                start++;
            }
            //Console.WriteLine("Starting End Iteration");
            for (int clueRight = Clue.Length - 1; clueRight > clueID; clueRight--)
            {
                //Console.WriteLine(IsX + ", " + LineID + ", " + clueID + ": " + Clue[clueRight] + ", " + end);
                end -= Clue[clueRight];
                if (Items[end] == SolutionState.True) end = GetGroupStartFromID(end);
                end--;
            }
            return new int[] { start, end };
        }
        private int GetSolvedClueCountFromStart()
        {
            int solved = 0;
            for (int i = LowerBound; i < Length; i++)
            {
                if (Items[i] == SolutionState.Empty) return solved;
                else if (Items[i] == SolutionState.True)
                {
                    if (!IsGroupAtID(i, SolutionState.True, SolutionState.False)) return solved;
                    else
                    {
                        i += GetGroupSizeFromID(i, SolutionState.True);
                        solved++;
                    }
                }
            }
            return solved;
        }
        private int GetSolvedClueCountFromEnd()
        {
            int solved = 0;
            for (int i = UpperBound; i >= LowerBound; i--)
            {
                if (Items[i] == SolutionState.Empty) return solved;
                else if (Items[i] == SolutionState.True)
                {
                    if (!IsGroupAtID(i, SolutionState.True, SolutionState.False)) return solved;
                    else
                    {
                        i -= GetGroupSizeFromID(i, SolutionState.True);
                        solved++;
                    }
                }
            }
            return solved;
        }

        private bool DoesSpaceContainTrueFromID(int id)
        {
            if (Items[id] == SolutionState.False) throw new Exception("Current ID is not a space block");
            int start = GetSpaceStartFromID(id), end = GetSpaceEndFromID(id);
            for (int i = start; i <= end; i++) if (Items[i] == SolutionState.True) return true;
            return false;
        }
        private bool IsGroupAtID(int id, SolutionState groupState, SolutionState surroundState)
        {
            // out of bounds returns true as legal surround state
            if (Items[id] != groupState) return false;
            int start = GetGroupStartFromID(id), end = GetGroupEndFromID(id);
            bool leftTrue = false, rightTrue = false;
            leftTrue = (IsOutsideBounds(start - 1) || Items[start - 1] == surroundState);
            rightTrue = (IsOutsideBounds(end + 1) || Items[end + 1] == surroundState);
            return leftTrue && rightTrue;
        }
        private int GetSpaceSizeFromID(int id)
        {
            if (Items[id] == SolutionState.False) return 0;
            else return GetSpaceEndFromID(id) - GetSpaceStartFromID(id) + 1;
        }
        private int GetGroupSizeFromID(int id, SolutionState groupState)
        {
            if (Items[id] != groupState) return 0;
            else return GetGroupEndFromID(id) - GetGroupStartFromID(id) + 1;
        }

        private bool IsOutsideBounds(int id)
        {
            return (id < LowerBound || id > UpperBound);
        }
        private int GetSpaceStartFromID(int id)
        {
            if (Items[id] == SolutionState.False) throw new Exception("Current ID is not a space block");
            int start = id;
            for (; start >= LowerBound; start--) if (Items[start] == SolutionState.False) return start + 1;
            return LowerBound;
        }
        private int GetSpaceEndFromID(int id)
        {
            if (Items[id] == SolutionState.False) throw new Exception("Current ID is not a space block");
            int end = id;
            for (; end <= UpperBound; end++) if (Items[end] == SolutionState.False) return end - 1;
            return UpperBound;
        }
        private int GetGroupStartFromID(int id)
        {
            SolutionState groupState = Items[id];
            int start = id;
            for (; start >= LowerBound; start--) if (Items[start] != groupState) return start + 1;
            return LowerBound;
        }
        private int GetGroupEndFromID(int id)
        {
            SolutionState groupState = Items[id];
            int end = id;
            for (; end <= UpperBound; end++) if (Items[end] != groupState) return end - 1;
            return UpperBound;
        }

        //===================================================================== PROPERTIES
        private bool IsSolved
        {
            get { return Items.Count(state => state == SolutionState.Empty) == 0; }
        }
        public int Length
        {
            get { return Items.Length; }
        }
        private int EffectiveLength
        {
            get { return EffectiveEnd - EffectiveStart + 1; }
        }
        private int LowerBound
        {
            get { return 0; }
        }
        private int UpperBound
        {
            get { return Length - 1; }
        }
        private int EffectiveStart
        {
            get
            {
                for (int i = LowerBound; i < Length; i++) if (Items[i] != SolutionState.False) return i;
                return LowerBound;
            }
        }
        private int EffectiveEnd
        {
            get
            {
                for (int i = Length - 1; i >= 0; i--)
                    if (Items[i] != SolutionState.False) return i;
                return Length - 1;
            }
        }
        private int UnsolvedStart
        {
            get
            {
                for (int i = LowerBound; i < Length; i++)
                {
                    if (Items[i] == SolutionState.Empty) return i;
                    else if (Items[i] == SolutionState.True && !IsGroupAtID(i, SolutionState.True, SolutionState.False)) return i;
                }
                return LowerBound;
            }
        }
        private int UnsolvedEnd
        {
            get
            {
                for (int i = UpperBound; i >= LowerBound; i--)
                {
                    if (Items[i] == SolutionState.Empty) return i;
                    else if (Items[i] == SolutionState.True && !IsGroupAtID(i, SolutionState.True, SolutionState.False)) return i;
                }
                return UpperBound;
            }
        }
        private int FirstClue
        {
            get { return Clue[0]; }
        }
        private int LastClue
        {
            get { return Clue[Clue.Length - 1]; }
        }
        private int FirstTrue
        {
            get
            {
                for (int i = 0; i < Length; i++) if (Items[i] == SolutionState.True) return i;
                return -1;
            }
        }
        private int LastTrue
        {
            get
            {
                for (int i = Items.Length - 1; i >= 0; i--) if (Items[i] == SolutionState.True) return i;
                return -1;
            }
        }
        private int LargestBlock
        {
            get
            {
                int largestBlock = 0, currentBlock = 0;
                for (int i = 0; i < Length; i++)
                {
                    if (Items[i] == SolutionState.True) currentBlock++;
                    else currentBlock = 0;
                    largestBlock = Math.Max(largestBlock, currentBlock);
                }
                return largestBlock;
            }
        }
    }
}
