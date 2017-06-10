using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Blackjack
{
    class Blackjack
    {
        public class BlackjackPlay
        {
            private Random deck;
            private int result; //1:player wins, 0:draw, -1:player loses
			private List<int> dealerCardsList;
			private List<int> playerCardsList;

            public BlackjackPlay(int Seed)
            {
                this.deck = new Random(Seed);
                this.result = 0; //結果の初期値は0
                this.dealerCardsList = new List<int>();
                this.playerCardsList = new List<int>();
            }

            //カードを一枚引く
            public int DrawCard()
            {
                int nextCard = deck.Next(1, 13);
                if (nextCard <= 9) {
                    return nextCard;
                } else {
                    return 10;
                }
            }

            //ゲーム開始時のディーリング
            public void InitDeal()
            {
                for (int i = 1; i <= 2; i++)
                {
                    dealerCardsList.Add(DrawCard());
                    playerCardsList.Add(DrawCard());
                }
            }

			public int TotalSumCalc(List<int> cardsList)
			{
				int totalSum = 0;
				int usableAce = 0;
                foreach (int card in cardsList)
                {
                    if (card == 1 && (totalSum + 11) <= 21)
                    {
                        totalSum += 11;
                        usableAce = 1;
                    }
                    else if (card == 1 && (totalSum + 11) > 21)
                    {
                        totalSum += 1;
                    }
                    else if (card > 1 && (totalSum + card) <= 21)
                    {
                        totalSum += card;
                    }
                    else if (card > 1 && (totalSum + card) > 21)
                    {
                        if (usableAce == 1)
                        {
                            totalSum += card - 10;
                            usableAce = 0;
                        }
                        else
                        {
                            totalSum += card;
                        }
                    }
                }
				return totalSum;
			}

            //カードのHit
            public void Hit(List<int> cardsList) {
                cardsList.Add(DrawCard());
            }

            //ゲームを開始する
            public void GameStart() {
                if (playerCardsList.Count == 2 && dealerCardsList.Count == 2) {
                    PlayersPolicy();
                } else {
                    Console.WriteLine("カードのディールが済んでいません");
                }
            }

            //プレイヤーの方策（プレイヤーターンの処理）
            public void PlayersPolicy() {
                while (true) {
                    if (TotalSumCalc(playerCardsList) > 21) {
                        result = -1;
                        GameResult(-1);
                        break;
                    }
                    else if (TotalSumCalc(playerCardsList) == 21 || TotalSumCalc(playerCardsList) == 20 ) {
                        DealersPolicy();
                        break;
                    } else {
                        Hit(playerCardsList);
                    }
                }
            }

            //ディーラーの方策（ディーラーターンの処理）
            public void DealersPolicy() {
                while (true) {
                    if (TotalSumCalc(dealerCardsList) > 21) {
                        GameResult(1);
                        break;
                    } else if (17 <= TotalSumCalc(dealerCardsList) && TotalSumCalc(dealerCardsList) <= 21) {
                        GameResult(0);
                        break;
                    } else {
                        Hit(dealerCardsList);
                    }
                }
            }

            //結果記録と出力を行うメソッド
            public void GameResult(int bustFlag) {
                if (bustFlag == 1) {
                    Console.WriteLine("ディーラーがバーストしました。プレイヤーの勝利です");
                    result = 1;
                } else if (bustFlag == -1) {
                    Console.WriteLine("プレイヤーがバーストしました。ディーラーの勝利です");
                    result = -1;
                } else if (bustFlag == 0) {
                    if (TotalSumCalc(playerCardsList) > TotalSumCalc(dealerCardsList)) {
                        Console.WriteLine("プレイヤーの勝利です");
                        result = 1;
                    } else if (TotalSumCalc(playerCardsList) > TotalSumCalc(dealerCardsList)) {
                        Console.WriteLine("ディーラーの勝利です");
                        result = -1;
                    } else {
                        Console.WriteLine("引き分けやで");
                        result = 0;
                    }
                }
                int counter = 1;
                foreach (var card in playerCardsList) {
                    Console.WriteLine("{0},{1}", counter, card);
                    counter++;
                }
                counter = 1;
                foreach (var card in dealerCardsList) {
					Console.WriteLine("{0},{1}", counter, card);
                    counter++;
				}
            }

            public int ResultFrag() {
                return result;
            }

            public List<int> PlayerCards() {
                return playerCardsList;
            }

            public List<int> DealerCards() {
                return dealerCardsList;
            }
        }

        public class ValueFunction
        {
            private List<int> FinalResultSet;
            private List<List<int>> PlayerCardsListSet;
            private List<List<int>> DealerCardsListSet;
            private List<StateRewardOccurence> StateRewardTable;
            private class StateRewardSet {
                public int dealerState;
                public int playerSumState;
                public int usableAceState;
                public int reward;

                public StateRewardSet(int dealerFaceUp, int playerSum, int usableAce, int rewardtype) {
                    this.dealerState = dealerFaceUp;
                    this.playerSumState = playerSum;
                    this.usableAceState = usableAce;
                    this.reward = rewardtype;
                }
            }
            private class StateRewardOccurence {
                public StateRewardSet srset;
                public int occurence;

                public StateRewardOccurence(StateRewardSet SRSet, int occ) {
                    this.srset = SRSet;
                    this.occurence = occ;
                }
            }

            public ValueFunction() {
                this.FinalResultSet = new List<int>();
                this.PlayerCardsListSet = new List<List<int>>();
                this.DealerCardsListSet = new List<List<int>>();
                this.StateRewardTable = new List<StateRewardOccurence>();
            }

            public void NewEpisode(int ResultFrag, List<int> PlayerCards, List<int> DealerCards) {
                FinalResultSet.Add(ResultFrag);
                PlayerCardsListSet.Add(PlayerCards);
                DealerCardsListSet.Add(DealerCards);
            }

            //エピソードセットから評価関数を計算
            public void Evaluation() {
                int dealersFaceUpCard;
                int usableAceFlag = 0;
                StateRewardSet SRSet;
                StateRewardOccurence SRO;
                for (int i = 0; i < FinalResultSet.Count; i++)
                {
                    dealersFaceUpCard = DealerCardsListSet[i][0];
                    int playerCardNum = 0;
                    int playersSum = 0;
                    foreach (var card in PlayerCardsListSet[i])
                    {
                        if (card == 1 && (playersSum + 11) <= 21)
                        {
                            playersSum += 11;
                            usableAceFlag = 1;
                        }
                        else if (card == 1 && (playersSum + 11) > 21)
                        {
                            playersSum += 1;
                        }
						else if (card > 1 && (playersSum + card) <= 21)
						{
							playersSum += card;
						}
                        else if (card > 1 && (playersSum + card) > 21)
                        {
                            if (usableAceFlag == 1)
                            {
                                playersSum += card - 10;
                                usableAceFlag = 0;
                            }
                            else
                            {
                                playersSum += card;
                            }
                        }

                        if (12 <= playersSum && playersSum <= 21)
                        {
                            Console.WriteLine("Start Evaluation!");
                            Console.WriteLine("dealersFaceUpCard: {0}, playersSum: {1}, usableAceFlag: {2}, result: {3}"
                                              , dealersFaceUpCard, playersSum, usableAceFlag, FinalResultSet[i]);
                            //現タイミングでヒットかスタンドのどちらを取っているかを判定。ヒットであれば報酬は0とする
                            //現タイミングがプレイヤーにとっての最後のカードではなく、かつバーストではなかったら
                            if (playerCardNum+1 < PlayerCardsListSet[i].Count && playersSum + PlayerCardsListSet[i][playerCardNum + 1] <= 21) {
                                SRSet = new StateRewardSet(dealersFaceUpCard, playersSum, usableAceFlag, 0);
                            } else {
                                SRSet = new StateRewardSet(dealersFaceUpCard, playersSum, usableAceFlag, FinalResultSet[i]);
                            }

                            if (StateRewardTable.Count == 0) {
                                SRO = new StateRewardOccurence(SRSet, 1);
                                StateRewardTable.Add(SRO);
                                Console.WriteLine("This is a First visit!");
                            } else {
                                bool existflag = false;
                                foreach (var sr in StateRewardTable) {
                                    //現在の状態がすでに登録されているかどうかをチェックし、登録されていれば上書き。そうでなければ新規登録
                                    if (sr.srset.dealerState == SRSet.dealerState && sr.srset.playerSumState == SRSet.playerSumState
                                        && sr.srset.usableAceState == SRSet.usableAceState) {
                                        sr.occurence ++;
                                        sr.srset.reward += SRSet.reward;
                                        existflag = true;
                                    }
                                }
                                if (!existflag) {
									SRO = new StateRewardOccurence(SRSet, 1);
									StateRewardTable.Add(SRO);
                                }
							}
                        }
                        playerCardNum++;
                    }
                }

                //結果の出力ルーティン
                var OrderedResult = StateRewardTable.OrderBy(a => a.srset.usableAceState)
                                                    .ThenBy(a => a.srset.dealerState).ThenBy(a => a.srset.playerSumState);
                int indentChecker = 0;
                foreach (var state in OrderedResult) {
                    if (indentChecker != 0 && state.srset.dealerState != indentChecker) {
                        Console.WriteLine("");
                    }
					Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}", state.srset.dealerState
									  , state.srset.playerSumState, state.srset.usableAceState
									  , state.occurence, state.srset.reward, (double)state.srset.reward / (double)state.occurence);
                    indentChecker = state.srset.dealerState;
                }
            }
        }
        static void Main(string[] args)
        {
            Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            int tryNumber = 500000;
            var ValueFunction = new ValueFunction();
            for (int i = 0; i < tryNumber;i++) {
				var Blackjack = new BlackjackPlay(i);
				Blackjack.InitDeal();
				Blackjack.GameStart();
				ValueFunction.NewEpisode(Blackjack.ResultFrag(), Blackjack.PlayerCards(), Blackjack.DealerCards());  
            }
            ValueFunction.Evaluation();

			sw.Stop();
			Console.WriteLine(sw.Elapsed);
		}
    }
}
