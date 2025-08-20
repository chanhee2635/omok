using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Server.Game
{
    public class OmokBoard
    {
        public StoneType[,] Board { get; private set; }
        public int BoardSize { get; private set; }

        // 돌 위치를 저장할 List
        private List<MoveInfo> Record = new List<MoveInfo>();
        // 금수 위치를 저장할 List
        private List<ForbiddenInfo> ForbiddenPoints = new List<ForbiddenInfo>();

        /// <summary>
        /// 생성자로 보드 초기화
        /// </summary>
        public OmokBoard(int size)
        {
            BoardSize = size;
            Board = new StoneType[size, size];

            BoardReset();
        }

        public void BoardReset()
        {
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    Board[r, c] = StoneType.ColorNone;
                }
            }

            Record.Clear();
            ForbiddenPoints.Clear();
        }

        /// <summary>
        /// 위치에 돌을 놓고 순서 저장
        /// </summary>
        public void SetStone(int c, int r, StoneType stone)
        {
            if (c >= 0 && c < BoardSize && r >= 0 && r < BoardSize)
            {
                Board[c, r] = stone;
            }

            Record.Add(new MoveInfo { Col = c, Row = r, Stone = stone, Turn = Record.Count + 1 });
        }

        public List<MoveInfo> GetRecord()
        {
            return Record; 
        }

        public List<ForbiddenInfo> GetForbiddenPoints()
        {
            return ForbiddenPoints;
        }

        int[][] directions = {
            new int[] {0, 1},   // 가로
            new int[] {1, 0},   // 세로
            new int[] {1, 1},   // 우하향 대각선 
            new int[] {1, -1}   // 우상향 대각선
        };

        /// <summary>
        /// 해당 위치에 오목이 되는지 확인
        /// </summary>
        public bool CheckWin(int c, int r, StoneType stone)
        {
            foreach (var dir in directions)
            {
                if (ConnectedCount(c, r, dir[0], dir[1], stone) == 5)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 돌을 놓았을 때 이어지는 돌 갯수
        /// </summary>
        private int ConnectedCount(int c, int r, int dc, int dr, StoneType stone)
        {
            int count = 1;

            int curC = c + dc;
            int curR = r + dr;
            while (curR >= 0 && curR < BoardSize && curC >= 0 && curC < BoardSize && Board[curC, curR] == stone)
            {
                count++;
                curC += dc;
                curR += dr;
            }

            curC = c - dc;
            curR = r - dr;
            while (curR >= 0 && curR < BoardSize && curC >= 0 && curC < BoardSize && Board[curC, curR] == stone)
            {
                count++;
                curC -= dc;
                curR -= dr;
            }

            return count;
        }

        /// <summary>
        /// 금수를 확인해서 저장 후 반환
        /// </summary>
        public List<ForbiddenInfo> CheckForbiddenMove()
        {
            ForbiddenPoints.Clear();

            // 보드판을 전부 돌면서 확인
            for (int i = 0; i < BoardSize; i++)
            {
                for (int j = 0; j < BoardSize; j++)
                {
                    if (Board[i, j] != 0)
                        continue;

                    // 장목
                    if (IsOverline(i, j))
                    {
                        ForbiddenInfo point = new ForbiddenInfo();
                        point.Col = i;
                        point.Row = j;
                        point.State = ForbiddenType.Overline;
                        ForbiddenPoints.Add(point);
                        continue;
                    }

                    // 44
                    if (IsDoubleFour(i, j))
                    {
                        ForbiddenInfo point = new ForbiddenInfo();
                        point.Col = i;
                        point.Row = j;
                        point.State = ForbiddenType.Doublefour;
                        ForbiddenPoints.Add(point);
                        continue;
                    }

                    // 33
                    if (IsDoubleThree(i, j))
                    {
                        ForbiddenInfo point = new ForbiddenInfo();
                        point.Col = i;
                        point.Row = j;
                        point.State = ForbiddenType.Doublethree;
                        ForbiddenPoints.Add(point);
                        continue;
                    }
                }
            }

            return ForbiddenPoints;
        }

        /// <summary>
        /// 금수 장목 확인
        /// </summary>
        private bool IsOverline(int col, int row)
        {
            // 확인을 위해 검은 돌을 먼저 놓기
            Board[col, row] = StoneType.ColorBlack;

            bool overline = false;

            // 혹시나 오목일 경우는 승리가 우선으로 4방향 모두 검사
            foreach (var dir in directions)
            {
                int count = ConnectedCount(col, row, dir[0], dir[1], StoneType.ColorBlack);

                if (count == 5)
                {
                    Board[col, row] = StoneType.ColorNone;
                    return false;
                }

                overline |= count > 5;
            }

            // 다시 빈 곳으로 돌려놓기
            Board[col, row] = StoneType.ColorNone;
            if (overline)
                Console.WriteLine($"장목입니다. ({col}, {row})");

            return overline;
        }

        /// <summary>
        /// 44 금수 확인
        /// </summary>
        private bool IsDoubleFour(int col, int row)
        {
            if (Board[col, row] != 0) return false;

            // 오목이 되는지 확인
            foreach (var dir in directions)
            {
                if (ConnectedCount(col, row, dir[0], dir[1], StoneType.ColorBlack) == 5)
                    return false;
            }

            int fourCount = 0;
            foreach (var dir in directions)
            {
                // 열린 44 확인
                if (IsOpenFour(col, row, dir[0], dir[1], StoneType.ColorBlack) == 2)
                    fourCount += 2;
                // 4 개수 확인
                else if (IsFour(col, row, dir[0], dir[1], StoneType.ColorBlack))
                    fourCount++;
            }

            return (fourCount >= 2);
        }

        /// <summary>
        /// 돌을 놓았을 때 연결 갯수가 4가 되는지 확인
        /// ○※●●●○○
        /// ○※●●○●○
        /// ○※●○●●○
        /// ○※○●●●○
        /// ○●※●●○○
        /// ○●※●○●○
        /// ○●※○●●○
        /// ○●●※●○○
        /// ○●●※○●○
        /// </summary>
        private bool IsFour(int col, int row, int dirC, int dirR, StoneType stone)
        {
            if (Board[col, row] != 0) return false;
            if (ConnectedCount(col, row, dirC, dirR, StoneType.ColorBlack) == 5) return false;
            if (stone == StoneType.ColorBlack && IsOverline(col, row)) return false;

            // 확인하기 위해 돌을 먼저 놓는다.
            Board[col, row] = stone;

            // 한쪽 방향으로 하나씩 이동해서 빈곳을 발견 후 돌을 놓았을 때 오목이 된다면 4의 위치
            int chkC = col + dirC;
            int chkR = row + dirR;

            while (chkC >= 0 && chkC < BoardSize && chkR >= 0 && chkR < BoardSize)
            {
                if (Board[chkC, chkR] == stone)
                {
                    chkC += dirC;
                    chkR += dirR;
                }
                else if (Board[chkC, chkR] == StoneType.ColorNone)
                {
                    // 오목이라면 놓을 수 있도록 함 
                    if (ConnectedCount(chkC, chkR, dirC, dirR, stone) == 5)
                    {
                        Board[col, row] = StoneType.ColorNone;
                        return true;
                    }
                    else
                        break;
                }
                else
                    break;
            }

            // 다른 한쪽으로 다시 이동 
            chkC = col - dirC;
            chkR = row - dirR;
            while (chkC >= 0 && chkC < BoardSize && chkR >= 0 && chkR < BoardSize && Board[chkC, chkR] == stone)
            {
                if (Board[chkC, chkR] == stone)
                {
                    chkC -= dirC;
                    chkR -= dirR;
                }
                else if (Board[chkC, chkR] == StoneType.ColorNone)
                {
                    // 오목이라면 놓을 수 있도록 함
                    if (ConnectedCount(chkC, chkR, dirC, dirR, stone) == 5)
                    {
                        Board[col, row] = StoneType.ColorNone;
                        return true;
                    }
                    else
                        break;
                }
                else
                    break;
            }

            Board[col, row] = StoneType.ColorNone;
            return false;
        }

        /// <summary>
        /// 한 줄에 띈 4가 두개 있는지 확인
        /// ○●○●※●○●○
        /// ○●○※●●○●○
        /// ○●●○●※○●●
        /// 
        /// 한쪽이 막혀있을 때
        /// ○●●●○※|
        /// ○●●○●※|
        /// ○●○●●※|
        /// ○○●●●※|
        /// </summary>
        int IsOpenFour(int col, int row, int dirC, int dirR, StoneType stone)
        {
            if (Board[col, row] != 0)
                return 0;
            if (ConnectedCount(col, row, dirC, dirR, StoneType.ColorBlack) == 5)
                return 0;
            if (stone == StoneType.ColorBlack && IsOverline(col, row))
                return 0;

            Board[col, row] = stone;

            int count = 1;
            int fCount = 0;

            // 한쪽 방향으로 이동하다 빈곳이 나오면 돌을 놨을 때 오목이 되는지 확인
            int chkC = col + dirC;
            int chkR = row + dirR;

            while (chkC >= 0 && chkC < BoardSize && chkR >= 0 && chkR < BoardSize)
            {
                if (Board[chkC, chkR] == stone)
                {
                    count++;
                    chkC += dirC;
                    chkR += dirR;
                }
                else if (Board[chkC, chkR] == StoneType.ColorNone)
                {
                    if (ConnectedCount(chkC, chkR, dirC, dirR, stone) != 5)
                    {
                        Board[col, row] = StoneType.ColorNone;
                        return 0;
                    }
                    else
                    {
                        fCount++;
                        break;
                    }
                }
                else
                {
                    Board[col, row] = StoneType.ColorNone;
                    return 0;
                }
            }

            // 다른 한쪽으로 이동해서 오목이 나오는 구간이 있다면
            // 이전 방향에 4가 있었고 또 4가 나왔기에 중복된 4가 아니라면 44이다.
            chkC = col - dirC;
            chkR = row - dirR;
            while (chkC >= 0 && chkC < BoardSize && chkR >= 0 && chkR < BoardSize)
            {
                if (Board[chkC, chkR] == stone)
                {
                    count++;
                    chkC -= dirC;
                    chkR -= dirR;
                }
                else if (Board[chkC, chkR] == StoneType.ColorNone)
                {
                    if (ConnectedCount(chkC, chkR, dirC, dirR, stone) == 5)
                    {
                        Board[col, row] = StoneType.ColorNone;

                        if (fCount == 0) return 1;
                        return (count == 4 ? 1 : 2);
                    }
                    else
                        break;
                }
                else
                    break;
            }

            Board[col, row] = StoneType.ColorNone;
            return 0;
        }

        /// <summary>
        /// 33 금수 확인
        /// </summary>
        private bool IsDoubleThree(int col, int row)
        {
            if (Board[col, row] != 0) return false;

            // 오목을 먼저 확인
            foreach (var dir in directions)
            {
                if (ConnectedCount(col, row, dir[0], dir[1], StoneType.ColorBlack) == 5)
                    return false;
            }

            // 3이 되는 갯수 확인
            int threeCount = 0;
            foreach (var dir in directions)
            {
                if (IsOpenThree(col, row, dir[0], dir[1], StoneType.ColorBlack))
                    threeCount++;
            }

            return (threeCount >= 2);

        }

        /// <summary>
        /// 돌을 놓았을 때 3이 되는지 확인
        /// ○※●●○○
        /// ○※●○●○
        /// ○※○●●○
        /// ○●※●○○
        /// ○●※○●○
        /// </summary>
        bool IsOpenThree(int col, int row, int dirC, int dirR, StoneType stone)
        {
            if (Board[col, row] != 0) return false;
            if (ConnectedCount(col, row, dirC, dirR, StoneType.ColorBlack) == 5) return false;
            if (stone == StoneType.ColorBlack && IsOverline(col, row)) return false;

            // 검증하기 위해 돌을 먼저 놓는다.
            Board[col, row] = stone;

            // 하나씩 이동해서 빈곳에 돌을 놓았을 때 4가 되고 해당 위치가 44금수나 33금수가 아니라면 3이 되는 위치
            int chkC = col + dirC;
            int chkR = row + dirR;

            while (chkC >= 0 && chkC < BoardSize && chkR >= 0 && chkR < BoardSize)
            {
                if (Board[chkC, chkR] == stone)
                {
                    chkC += dirC;
                    chkR += dirR;
                }
                else if (Board[chkC, chkR] == StoneType.ColorNone)
                {
                    if ((IsOpenFour(chkC, chkR, dirC, dirR, stone) == 1) && (!IsDoubleFour(chkC, chkR)) && (!IsDoubleThree(chkC, chkR)))
                    {
                        Board[col, row] = StoneType.ColorNone;
                        return true;
                    }
                    else
                        break;
                }
                else
                    break;
            }

            chkC = col - dirC;
            chkR = row - dirR;
            while (chkC >= 0 && chkC < BoardSize && chkR >= 0 && chkR < BoardSize)
            {
                if (Board[chkC, chkR] == stone)
                {
                    chkC -= dirC;
                    chkR -= dirR;
                }
                else if (Board[chkC, chkR] == StoneType.ColorNone)
                {
                    if ((IsOpenFour(chkC, chkR, dirC, dirR, stone) == 1) && (!IsDoubleFour(chkC, chkR)) && (!IsDoubleThree(chkC, chkR)))
                    {
                        Board[col, row] = StoneType.ColorNone;
                        return true;
                    }
                    else
                        break;
                }
                else
                    break;
            }

            Board[col, row] = StoneType.ColorNone;
            return false;
        }

    }
}
