//#define _ML
//#define _AI2
//#define _DoE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Point = System.Drawing.Point;

namespace CornerkickUnitTest
{
  [TestClass]
  public class UnitTestGame
  {
    CornerkickGame.Game game = new CornerkickGame.Game(new CornerkickGame.Game.Data());

    [TestMethod]
    public void TestOffsite()
    {
      for (byte iHA = 0; iHA < 2; iHA++) {
        CornerkickGame.Game gameTest = game.tl.getDefaultGame();

        CornerkickGame.Player plOffsite = gameTest.player[iHA][10];

        // not offsite
        plOffsite.ptPos.X = gameTest.player[1 - iHA][2].ptPos.X;
        Assert.AreEqual(false, gameTest.tl.checkPlayerIsOffsite(plOffsite), "Player is offsite!");

        // offsite
        if (iHA == 0) plOffsite.ptPos.X++;
        else          plOffsite.ptPos.X--;
        Assert.AreEqual(true, gameTest.tl.checkPlayerIsOffsite(plOffsite), "Player is not offsite!");
      }
    }

    [TestMethod]
    public void TestShoot()
    {
      string[] sHA = { "Home", "Away" };
      for (byte iHA = 0; iHA < 2; iHA++) {
        CornerkickGame.Game gameTest = game.tl.getDefaultGame();
        gameTest.next();

        CornerkickGame.Player plShoot  = gameTest.player[iHA][10];
        CornerkickGame.Player plKeeper = gameTest.tl.getKeeper(iHA == 1);

        // chance
        resetPlayerShoot(plShoot, plKeeper, gameTest, iHA);
        float[] fDist = CornerkickGame.Tool.getDistanceToGoal(plShoot.ptPos, iHA == 0, gameTest.ptPitch.X, gameTest.fConvertDist2Meter);
        Assert.AreEqual(0.52828, CornerkickGame.AI.getChanceShootOnGoal(7f, fDist, iHA == 0, plShoot.iLookAt), 0.0001, "ChanceShootOnGoal");

        float[] fKeeper = gameTest.getKeeperSkills(plKeeper, plShoot);
        float[] fShoot  = gameTest.getShootSkills(plShoot);
        // 0.69367641210556
        Assert.AreEqual(0.6936764121, CornerkickGame.AI.getChanceShootKeeperSave(fKeeper, fDist, fShoot), 0.0001, "ChanceKeeperSave");

        // aside (0)
        resetPlayerShoot(plShoot, plKeeper, gameTest, iHA);
        gameTest.doShoot(plShoot, 0, 0);
        if (iHA == 0) Assert.AreEqual(true, gameTest.ball.ptPos.X > gameTest.ptPitch.X, "Ball is not away out!");
        else          Assert.AreEqual(true, gameTest.ball.ptPos.X < gameTest.ptPitch.X, "Ball is not home out!");
        gameTest.next();
        Assert.AreEqual(6, Math.Abs(gameTest.iStandard), "Standard is not Goal-off!");

        // goal (1)
        resetPlayerShoot(plShoot, plKeeper, gameTest, iHA);
        int iGoalH = gameTest.data.team[0].iGoals;
        int iGoalA = gameTest.data.team[1].iGoals;
        gameTest.doShoot(plShoot, 0, 1);
        testGoal(gameTest, iHA, iGoalH, iGoalA);

        // save (2)
        resetPlayerShoot(plShoot, plKeeper, gameTest, iHA);
        gameTest.doShoot(plShoot, 0, 2);
        Assert.AreEqual(plKeeper.ptPos.X, gameTest.ball.ptPos.X, "Ball is not at away keeper!");
        Assert.AreEqual(plKeeper.ptPos.Y, gameTest.ball.ptPos.Y, "Ball is not at home keeper!");

        // cornerkick (4)
        resetPlayerShoot(plShoot, plKeeper, gameTest, iHA);
        gameTest.doShoot(plShoot, 0, 4);
        if (iHA == 0) Assert.AreEqual(gameTest.ptPitch.X + 4, gameTest.ball.ptPos.X, "Ball is not away out!");
        else          Assert.AreEqual(                   - 4, gameTest.ball.ptPos.X, "Ball is not home out!");
        gameTest.next();
        Assert.AreEqual(3, Math.Abs(gameTest.iStandard), "Standard is not cornerkick!");

        //for (int iS = gameTest.iStandardCounter; iS >= 0; iS--) {
        while (gameTest.iStandardCounter > 0) {
          //Assert.AreEqual(gameTest.iStandardCounter, iS, "StandardCounter is not " + iS.ToString() + " but " + gameTest.iStandardCounter.ToString() + "!");
        
          Assert.AreEqual(gameTest.ptPitch.X * (1 - iHA),          gameTest.ball.ptPos.X,  "Ball X is not at " + sHA[1 - iHA] + " corner!");
          Assert.AreEqual(gameTest.ptPitch.Y,             Math.Abs(gameTest.ball.ptPos.Y), "Ball Y is not at " + sHA[1 - iHA] + " corner!");

          gameTest.next();
        }
      }
    }
    private void resetPlayerShoot(CornerkickGame.Player plShoot, CornerkickGame.Player plKeeper, CornerkickGame.Game gameTest, byte iHA)
    {
      plShoot.ptPos.X = (int)Math.Round((gameTest.ptPitch.X * 0.8) - (iHA * gameTest.ptPitch.X * 0.6));
      plShoot.ptPos.Y = (iHA * 2) - 1;
      plShoot .fSteps = 7f;
      plShoot.iLookAt = (byte)(3 - (iHA * 3));
      plKeeper.fSteps = 7f;
      gameTest.ball.ptPos = plShoot.ptPos;
      gameTest.ball.plAtBall     = plShoot;
      gameTest.ball.plAtBallLast = plShoot;
      gameTest.iStandard = 0;
    }

    [TestMethod]
    public void TestPass()
    {
      for (byte iHA = 0; iHA < 2; iHA++) {
        CornerkickGame.Game gameTest = game.tl.getDefaultGame();

        gameTest.next();
        while (gameTest.iStandardCounter > 0) gameTest.next();

        CornerkickGame.Player plPass = gameTest.player[iHA][10];
        plPass.ptPos = new Point(gameTest.ptPitch.X / 2, 11);
        plPass.iLookAt = (byte)(3 - (3 * iHA));
        gameTest.ball.ptPos = plPass.ptPos;

        gameTest.doPass(plPass, new Point(gameTest.ptPitch.X * (1 - iHA), plPass.ptPos.Y));
        gameTest.ball.iPassStep  = 2;
        gameTest.ball.nPassSteps = gameTest.ball.iPassStep;

        while (gameTest.ball.iPassStep > 0) gameTest.next();
        gameTest.next();

        Assert.AreEqual(6, Math.Abs(gameTest.iStandard), "Standard is not Goal-kick!");
      }

      for (byte iHA = 0; iHA < 2; iHA++) {
        CornerkickGame.Game gameTest = game.tl.getDefaultGame();

        gameTest.next();
        while (gameTest.iStandardCounter > 0) gameTest.next();

        CornerkickGame.Player plPass = gameTest.player[iHA][10];
        plPass.ptPos = new Point(gameTest.ptPitch.X / 2, 11);
        plPass.iLookAt = (byte)(3 - (3 * iHA));
        gameTest.ball.ptPos = plPass.ptPos;

        gameTest.doPass(plPass, new Point(plPass.ptPos.X, gameTest.ptPitch.Y * ((2 * iHA) - 1)));
        gameTest.ball.iPassStep  = 2;
        gameTest.ball.nPassSteps = gameTest.ball.iPassStep;

        while (gameTest.ball.iPassStep > 0) gameTest.next();
        gameTest.next();

        Assert.AreEqual(4, Math.Abs(gameTest.iStandard), "Standard is not throw-in!");

        while (gameTest.iStandardCounter > 0) gameTest.next();
        
        Assert.AreEqual(0, gameTest.iStandard, "Standard is not 0!");
      }
    }

    [TestMethod]
    public void TestPass2()
    {
      for (byte iHA = 0; iHA < 2; iHA++) {
        CornerkickGame.Game gameTest = game.tl.getDefaultGame();

        gameTest.doStart();
        //while (gameTest.iStandardCounter > 0) gameTest.next();

        gameTest.ball.plAtBall = gameTest.player[iHA][1];

        List<double> fPassChance;
        List<CornerkickGame.Player> ltReceiver = gameTest.ai.getReceiverList(gameTest.ball.plAtBall, out fPassChance);

        /*
        Assert.AreEqual(7, ltReceiver .Count);
        Assert.AreEqual(7, fPassChance.Count);
        Assert.AreEqual(0.01814744747199897, fPassChance[0], 0.00001);
        Assert.AreEqual(0.02274913207528134, fPassChance[1], 0.00001);
        Assert.AreEqual(0.03720340828571097, fPassChance[2], 0.00001);
        Assert.AreEqual(0.09581509307625481, fPassChance[3], 0.00001);
        Assert.AreEqual(0.22830368032279283, fPassChance[4], 0.00001);
        Assert.AreEqual(0.28957488265690384, fPassChance[5], 0.00001);
        Assert.AreEqual(0.30820635611105718, fPassChance[6], 0.00001);
        */

        Assert.AreEqual(9, ltReceiver .Count);
        Assert.AreEqual(9, fPassChance.Count);
        Assert.AreEqual(0.014182744704188292, fPassChance[0], 0.00001);
        Assert.AreEqual(0.017779091685671454, fPassChance[1], 0.00001);
        Assert.AreEqual(0.029075518342514362, fPassChance[2], 0.00001);
        Assert.AreEqual(0.055173570726610936, fPassChance[3], 0.00001);
        Assert.AreEqual(0.074882211727315351, fPassChance[4], 0.00001);
        Assert.AreEqual(0.163298077913128360, fPassChance[5], 0.00001);
        Assert.AreEqual(0.178425798892152270, fPassChance[6], 0.00001);
        Assert.AreEqual(0.226310980638191000, fPassChance[7], 0.00001);
        Assert.AreEqual(0.240872005370227930, fPassChance[8], 0.00001);
        
        //CornerkickGame.Player plRec = gameTest.ai.getReceiver(plAtBall);
      }
    }

    [TestMethod]
    public void TestDuel()
    {
      for (byte iHA = 0; iHA < 2; iHA++) {
        CornerkickGame.Game gameTest = game.tl.getDefaultGame();

        gameTest.next();
        while (gameTest.iStandardCounter > 0) gameTest.next();

        CornerkickGame.Player plDef = gameTest.player[    iHA][ 1];
        CornerkickGame.Player plOff = gameTest.player[1 - iHA][10];

        gameTest.ball.ptPos = plOff.ptPos;
        gameTest.ball.plAtBall = plOff;
        plDef.ptPos.X = plOff.ptPos.X - (2 - (iHA * 4));

        gameTest.doDuel(plDef, 3);

        Assert.AreEqual(2, Math.Abs(gameTest.iStandard), "Standard is not freekick!");
      }
    }

    [TestMethod]
    public void TestDuel2()
    {
      int iDuelH = 0;
      int iDuelA = 0;

      const int nDuels = 10000;
      for (int iD = 0; iD < nDuels; iD++) {
        for (byte iHA = 0; iHA < 2; iHA++) { // iHA: plDef
          CornerkickGame.Game gameTest = game.tl.getDefaultGame();

          gameTest.next();
          while (gameTest.iStandardCounter > 0) gameTest.next();

          CornerkickGame.Player plDef = gameTest.player[    iHA][ 1];
          CornerkickGame.Player plOff = gameTest.player[1 - iHA][10];

          plOff.ptPos.X = (int)(gameTest.ptPitch.X * 0.25);
          if (iHA > 0) plOff.ptPos.X = gameTest.ptPitch.X - plOff.ptPos.X;
          plDef.ptPos.Y = 0;
          plOff.iLookAt = (byte)(3 * iHA);

          gameTest.ball.ptPos = plOff.ptPos;
          gameTest.ball.plAtBall = plOff;

          plDef.ptPos.X = plOff.ptPos.X - (2 - (iHA * 4));
          plDef.ptPos.Y = plOff.ptPos.Y;
          plDef.iLookAt = (byte)(3 - (3 * iHA));

          int iDuelRes = gameTest.doDuel(plDef);
          if (iDuelRes >= 0) {
            if (iHA == 0) iDuelH++;
            else          iDuelA++;
          }
        }
      }

      Assert.AreEqual(0.966185451, iDuelH / (double)nDuels, 0.01);
      Assert.AreEqual(0.966185451, iDuelA / (double)nDuels, 0.01);
    }

    [TestMethod]
    public void TestChances3vs1()
    {
      CornerkickGame.Game gameTest = game.tl.getDefaultGame();

      gameTest.next();
      while (gameTest.iStandardCounter > 0) gameTest.next();

      for (byte jHA = 0; jHA < 2; jHA++) {
        for (byte iPl = 0; iPl < gameTest.player[jHA].Length; iPl++) {
          CornerkickGame.Player pl = gameTest.player[jHA][iPl];
          if (gameTest.tl.checkPlayerIsKeeper(pl)) continue;

          pl.ptPos.X = Math.Min(pl.ptPos.X, gameTest.ptPitch.X / 2); // Place player at middle line
        }
      }

      CornerkickGame.Player plDef  = gameTest.player[1][ 1];
      CornerkickGame.Player plOff1 = gameTest.player[0][ 8];
      CornerkickGame.Player plOff2 = gameTest.player[0][ 9];
      CornerkickGame.Player plOff3 = gameTest.player[0][10];
      CornerkickGame.Player plReceiver = null;

      plOff1.iLookAt = 3;
      plOff2.iLookAt = 3;
      plOff3.iLookAt = 3;

      plDef.ptPos.Y = 0;
      plDef.iLookAt = 0;

      float[] fChance = new float[4] { 0.00160853565f, 0.020464249f, 0.141618967f, 0.4702977f };
      for (int iRelDist = 0; iRelDist < fChance.Length; iRelDist++) {
        float fRelDist = 0.6f + (iRelDist * 0.1f);

        plOff1.ptPos.X = (int)(gameTest.ptPitch.X * fRelDist);
        plOff2.ptPos.X = (int)(gameTest.ptPitch.X * fRelDist);
        plOff3.ptPos.X = (int)(gameTest.ptPitch.X * fRelDist);

        plDef.ptPos.X = (int)(gameTest.ptPitch.X * 0.9);

        gameTest.ball.ptPos = plOff2.ptPos;
        gameTest.ball.plAtBall = plOff2;

        float[] fPlAction = gameTest.ai.getPlayerAction(plOff2, out plReceiver, false, 10);
        //Assert.AreEqual(fChance[iRelDist], fPlAction[0], 0.002, "ChanceShoot");
      }

      gameTest.iStandard = 2;
      float[] fPlActionFreekick = gameTest.ai.getPlayerAction(plOff2, out plReceiver, false, 10);
      //Assert.AreEqual(0.0340206772, fPlActionFreekick[0], 0.2, "ChanceShoot Freekick");
      gameTest.next();
    }

    [TestMethod]
    public void TestPhi()
    {
      int iX0 = game.ptPitch.X / 2;
      int iY0 = 0;
      Point pt0 = new Point(iX0, iY0);
      Assert.AreEqual(  0.0, game.tl.getPhi(pt0, new Point(pt0.X - 1, pt0.Y    )), 0.001); // A
      Assert.AreEqual( 60.0, game.tl.getPhi(pt0, new Point(pt0.X - 1, pt0.Y - 1)), 0.001); // W
      Assert.AreEqual(120.0, game.tl.getPhi(pt0, new Point(pt0.X + 1, pt0.Y - 1)), 0.001); // E
      Assert.AreEqual(180.0, game.tl.getPhi(pt0, new Point(pt0.X + 1, pt0.Y    )), 0.001); // D
      Assert.AreEqual(240.0, game.tl.getPhi(pt0, new Point(pt0.X + 1, pt0.Y + 1)), 0.001); // X
      Assert.AreEqual(300.0, game.tl.getPhi(pt0, new Point(pt0.X - 1, pt0.Y + 1)), 0.001); // Y
    }

    [TestMethod]
    public void TestPenalty()
    {
      const int nPenalties = 20000;

      for (byte iHA = 0; iHA < 2; iHA++) {
        int iG = 0;
        for (int iP = 0; iP < nPenalties; iP++) {
          CornerkickGame.Game gameTest = game.tl.getDefaultGame();
          gameTest.next();
          gameTest.iStandard = 0;
          
          CornerkickGame.Player plDef = gameTest.player[1 - iHA][ 1];
          CornerkickGame.Player plOff = gameTest.player[    iHA][10];

          if (iHA == 0) plDef.ptPos = new Point(gameTest.ptPitch.X - (gameTest.ptBox.X / 2), 0);
          else          plDef.ptPos = new Point(                      gameTest.ptBox.X / 2 , 0);
          plOff.ptPos = plDef.ptPos;
          if (iHA == 0) plOff.ptPos.X += 2;
          else          plOff.ptPos.X -= 2;

          gameTest.ball.ptPos    = plOff.ptPos;
          gameTest.ball.plAtBall = plOff;

          gameTest.doDuel(plDef, 3);

          // Check that standard is penalty
          Assert.AreEqual(1, Math.Abs(gameTest.iStandard));

          while (gameTest.iStandardCounter > 0) {
            gameTest.next();
          }

          // Check that penalty is not set anymore
          Assert.AreEqual(false, Math.Abs(gameTest.iStandard) == 1);

          iG += gameTest.data.team[iHA].iGoals;
        }

        // Check penalty success (82% - 84%)
        Assert.AreEqual(0.83, iG / (float)nPenalties, 0.02);
      }
    }

    [TestMethod]
    public void TestShootout()
    {
      CornerkickGame.Game gameTest = game.tl.getDefaultGame();
      gameTest.next();
      gameTest.iStandard = 0;
      gameTest.tsMinute = new TimeSpan(2, 0, 0); // Set game time to 120 min.
      gameTest.data.bFirstHalf  = false;
      gameTest.data.bOtPossible = true;
      gameTest.data.bOvertime   = true;
      
      // Loop until shootout
      while (!gameTest.data.bShootout) gameTest.next();

      int iShootoutCounter = 0;
      int iHA = -1;
      while (gameTest.next() > 0) {
        if (gameTest.data.bShootout) {
          if (iShootoutCounter % 2 == 0) {
            if (iHA < 0) iHA = gameTest.ball.plAtBall.iHA;

            Assert.AreEqual(gameTest.iPenaltyX, gameTest.ball.ptPos.X);
            Assert.AreEqual(                 0, gameTest.ball.ptPos.Y);
            Assert.AreEqual(gameTest.ball.ptPos, gameTest.ball.plAtBall.ptPos);

            Assert.AreEqual(iHA, gameTest.ball.plAtBall.iHA);

            iHA = 1 - iHA;
          }

          iShootoutCounter++;
        }
      }

      Assert.AreEqual(true, gameTest.data.team[0].iGoals != gameTest.data.team[1].iGoals);
    }

    [TestMethod]
    public void TestGamesDirect()
    {
      const int nGames = 1000;
      const float fRefereeCorruptHome = 0f;

      CornerkickManager.Main mn = new CornerkickManager.Main();

#if _DoE
      float fWf1 = 0.5f;
      const float fWfStep = 0.2f;
      const float fWfMax  = 2.0f;

      StreamWriter swLog = new StreamWriter(mn.sHomeDir + "/Test_Results_DoE.txt", false);
      swLog.WriteLine("Wf1 Wf2 chance_goal_H chance_goal_A H/A");
      swLog.Close();

      while (fWf1 < fWfMax) { // First loop
        float fWf2 = 0.5f;

        while (fWf2 < fWfMax) { // Second loop
#endif
          int iGH = 0;
          int iGA = 0;
          int iShootsH = 0;
          int iShootsA = 0;
          double fChanceGoalH = 0.0;
          double fChanceGoalA = 0.0;
          double fShootDistH = 0.0;
          double fShootDistA = 0.0;
          int iV = 0;
          int iD = 0;
          int iL = 0;
          uint iStepsH = 0;
          uint iStepsA = 0;
          uint iDuelH = 0;
          uint iDuelA = 0;
          uint iPossH = 0;
          uint iPossA = 0;
          uint iPassH = 0;
          uint iPassA = 0;
          uint iOffsiteH = 0;
          uint iOffsiteA = 0;

#if _ML
          double fWfPass = 1f;
          double fWfPassCounter = 0;
#endif

          for (int iG = 0; iG < nGames; iG++) {
            CornerkickGame.Game gameTest = game.tl.getDefaultGame();
            gameTest.data.bInjuriesPossible = false;
            gameTest.data.bCardsPossible = false;

            // Corrupt referee
            gameTest.data.team[0].fRefereeCorrupt = fRefereeCorruptHome;
#if _ML
            gameTest.ai.ml.fWfPass = fWfPass;
            gameTest.ai.ml.fWfPassCounter = fWfPassCounter;
#endif
#if _DoE
            gameTest.ai.fWfChancePass = fWf1;
            gameTest.ai.fWfChanceSolo = fWf2;
#endif

            gameTest.next();

            const int iBallCounterMax = 100;
            int iBallCounter = 0;
            Point ptBall = gameTest.ball.ptPos;

            int iGoalH = 0;
            int iGoalA = 0;
            while (gameTest.next() > 0) {
              if (gameTest.tl.correctPos(ref gameTest.ball.ptPos)) gameTest.tl.writeState();

              Assert.AreEqual(false, gameTest.tl.correctPos(ref gameTest.ball.ptPos), "Ball Position [" + ptBall.X + "/" + ptBall.Y + "] corrected. (Minute: " + gameTest.tsMinute.ToString() + ")");
        
              if (gameTest.ball.ptPos == ptBall) {
                iBallCounter++;

                if (iBallCounter >= iBallCounterMax) {
                  gameTest.tl.writeState();

                  for (byte jNext = 0; jNext < 50; jNext++) {
                    gameTest.next(); // for debugging
                  }
                }
          
                Assert.AreEqual(true, iBallCounter < iBallCounterMax, "Ball is at Positon: [" + ptBall.X + "/" + ptBall.Y + "] for " + iBallCounter.ToString() + " times (Minute: " + gameTest.tsMinute.ToString() + ")");
          
                if (iBallCounter >= iBallCounterMax) break;
              } else {
                iBallCounter = 0;
                ptBall = gameTest.ball.ptPos;
              }

              // Test goal if goals difer from last step
              if (iGoalH != gameTest.data.team[0].iGoals ||
                  iGoalA != gameTest.data.team[1].iGoals) {
                testGoal(gameTest, iGoalH != gameTest.data.team[0].iGoals, iGoalH, iGoalA);
              }

              iGoalH = gameTest.data.team[0].iGoals;
              iGoalA = gameTest.data.team[1].iGoals;

#if !_DoE
              if ((int)gameTest.tsMinute.TotalMinutes % 10 == 0 && gameTest.tsMinute.Seconds == 0) testHA(gameTest);
#endif
            }

#if _ML
            fWfPass  = gameTest.ai.ml.fWfPass;
            fWfPassCounter += gameTest.ai.ml.fWfPassCounter;

            Debug.WriteLine(" Game " + (iG + 1).ToString()  + ". Result: " + gameTest.data.team[0].iGoals.ToString() + ":" + gameTest.data.team[1].iGoals.ToString() + ", " + gameTest.data.tsMinute.TotalMinutes.ToString("0") + ":" + gameTest.data.tsMinute.Seconds.ToString("00") + ", WfPass: " + fWfPass.ToString("0.0000"));
#else
            Debug.WriteLine(" Game " + (iG + 1).ToString()  + ". Result: " + gameTest.data.team[0].iGoals.ToString() + ":" + gameTest.data.team[1].iGoals.ToString() + ", " + gameTest.data.tsMinute.TotalMinutes.ToString("0") + ":" + gameTest.data.tsMinute.Seconds.ToString("00"));
#endif

            // Check player experience
            for (byte iHA = 0; iHA < 2; iHA++) {
              foreach (CornerkickGame.Player pl in gameTest.player[iHA]) Assert.AreEqual(true, pl.fExperience > 0f);
            }

            // Count data
            iGH += gameTest.data.team[0].iGoals;
            iGA += gameTest.data.team[1].iGoals;

            List<CornerkickGame.Game.Shoot> ltShootsH = mn.ui.getShoots(gameTest.data.ltState, 0);
            iShootsH += ltShootsH.Count;
            foreach (CornerkickGame.Game.Shoot shoot in ltShootsH) {
              fChanceGoalH += CornerkickGame.AI.getChanceShootGoal(shoot);
              fShootDistH  += CornerkickGame.Tool.getDistanceToGoal(shoot.plShoot, gameTest.ptPitch.X, gameTest.fConvertDist2Meter)[0];
            }

            List<CornerkickGame.Game.Shoot> ltShootsA = mn.ui.getShoots(gameTest.data.ltState, 1);
            iShootsA += ltShootsA.Count;
            foreach (CornerkickGame.Game.Shoot shoot in ltShootsA) {
              fChanceGoalA += CornerkickGame.AI.getChanceShootGoal(shoot);
              fShootDistA  += CornerkickGame.Tool.getDistanceToGoal(shoot.plShoot, gameTest.ptPitch.X, gameTest.fConvertDist2Meter)[0];
            }

            iDuelH += (uint)mn.ui.getDuels(gameTest.data.ltState, 0).Count;
            iDuelA += (uint)mn.ui.getDuels(gameTest.data.ltState, 1).Count;

            for (byte iPl = 0; iPl < gameTest.player[0].Length; iPl++) iStepsH += (uint)gameTest.player[0][iPl].iSteps;
            for (byte iPl = 0; iPl < gameTest.player[1].Length; iPl++) iStepsA += (uint)gameTest.player[1][iPl].iSteps;

            iPossH += (uint)gameTest.data.team[0].iPossession;
            iPossA += (uint)gameTest.data.team[1].iPossession;

            List<CornerkickGame.Game.Pass> lPassesH = mn.ui.getPasses(gameTest.data.ltState, 0);
            iPassH += (uint)lPassesH.Count;
            List<CornerkickGame.Game.Pass> lPassesA = mn.ui.getPasses(gameTest.data.ltState, 1);
            iPassA += (uint)lPassesA.Count;

            iOffsiteH += (uint)gameTest.data.team[0].iOffsite;
            iOffsiteA += (uint)gameTest.data.team[1].iOffsite;

            if      (gameTest.data.team[0].iGoals > gameTest.data.team[1].iGoals) iV++;
            else if (gameTest.data.team[0].iGoals < gameTest.data.team[1].iGoals) iL++;
            else                                                                  iD++;
          } // for each game

          fChanceGoalH /= nGames;
          fChanceGoalA /= nGames;

          fShootDistH /= iShootsH;
          fShootDistA /= iShootsA;

          Debug.WriteLine("");
          Debug.WriteLine("OOOOOOOOOOOOOOOOOOOOOOOO");
          Debug.WriteLine("OOO    Statistics    OOO");
          Debug.WriteLine("OOOOOOOOOOOOOOOOOOOOOOOO");
          Debug.WriteLine("        goals: " + ((iGH          + iGA)          / (2.0 * nGames)).ToString("0.0000") + " / " + (iGH          / (double)iGA)         .ToString("0.0000"));
          Debug.WriteLine("  chance goal: " + ((fChanceGoalH + fChanceGoalA) /  2.0          ).ToString("0.0000") + " / " + (fChanceGoalH /         fChanceGoalA).ToString("0.0000"));
          Debug.WriteLine("       shoots: " + ((iShootsH     + iShootsA)     / (2.0 * nGames)).ToString("0.0000") + " / " + (iShootsH     / (double)iShootsA)    .ToString("0.0000"));
          Debug.WriteLine("  shoot dist.: " + ((fShootDistH  + fShootDistA)  /  2.0          ).ToString("0.000")  + " / " + (fShootDistH  /         fShootDistA) .ToString("0.0000"));
          Debug.WriteLine("        duels: " + ((iDuelH       + iDuelA)       / (2.0 * nGames)).ToString("0.000")  + " / " + (iDuelH       / (double)iDuelA)      .ToString("0.0000"));
          Debug.WriteLine("        steps: " + ((iStepsH      + iStepsA)      / (2.0 * nGames)).ToString("0 ")     + " / " + (iStepsH      / (double)iStepsA)     .ToString("0.0000"));
          Debug.WriteLine("   possession: " + ((iPossH       + iPossA)       / (2.0 * nGames)).ToString("0.0")    + " / " + (iPossH       / (double)iPossA)      .ToString("0.0000"));
          Debug.WriteLine("       passes: " + ((iPassH       + iPassA)       / (2.0 * nGames)).ToString("0.00")   + " / " + (iPassH       / (double)iPassA)      .ToString("0.0000"));
          Debug.WriteLine("     offsites: " + ((iOffsiteH    + iOffsiteA)    / (2.0 * nGames)).ToString("0.0000") + " / " + (iOffsiteH    / (double)iOffsiteA)   .ToString("0.0000"));

          Debug.WriteLine("Total: " + iGH.ToString() + ":" + iGA.ToString());
          Debug.WriteLine("Avrge: " + (iGH / (float)nGames).ToString("0.00") + ":" + (iGA / (float)nGames).ToString("0.00"));
          Debug.WriteLine("Win Home/Draw/Away: " + iV.ToString() + "/" + iD.ToString() + "/" + iL.ToString());
#if _ML
          Debug.WriteLine("fWfPass: " + fWfPass.ToString("0.0000"));
#endif

          // Write results to file
#if _DoE
          swLog = new StreamWriter(mn.sHomeDir + "/Test_Results_DoE.txt", true);
          swLog.WriteLine(fWf1.ToString("0.00") + " " + fWf2.ToString("0.00") + " " + fChanceGoalH.ToString("0.0000").PadLeft(9) + " " + fChanceGoalA.ToString("0.0000").PadRight(9) + " " + (fChanceGoalH / fChanceGoalA).ToString());
#else
          StreamWriter swLog = new StreamWriter(mn.sHomeDir + "/Test_Results.txt", true);
          swLog.WriteLine("Start date: " + DateTime.Now + ". Performed games: " + nGames.ToString());
          swLog.WriteLine("      goals: " + iGH         .ToString(        ).PadLeft(9) + " / " + iGA         .ToString(        ).PadRight(9) + ": " + (iGH          / (double)iGA)         .ToString());
          swLog.WriteLine("chance goal: " + fChanceGoalH.ToString("0.0000").PadLeft(9) + " / " + fChanceGoalA.ToString("0.0000").PadRight(9) + ": " + (fChanceGoalH /         fChanceGoalA).ToString());
          swLog.WriteLine("     shoots: " + iShootsH    .ToString(        ).PadLeft(9) + " / " + iShootsA    .ToString(        ).PadRight(9) + ": " + (iShootsH     / (double)iShootsA)    .ToString());
          swLog.WriteLine("shoot dist.: " + fShootDistH .ToString("0.0000").PadLeft(9) + " / " + fShootDistA .ToString("0.0000").PadRight(9) + ": " + (fShootDistH  /         fShootDistA) .ToString());
          swLog.WriteLine("      duels: " + iDuelH      .ToString(        ).PadLeft(9) + " / " + iDuelA      .ToString(        ).PadRight(9) + ": " + (iDuelH       / (double)iDuelA)      .ToString());
          swLog.WriteLine("      steps: " + iStepsH     .ToString(        ).PadLeft(9) + " / " + iStepsA     .ToString(        ).PadRight(9) + ": " + (iStepsH      / (double)iStepsA)     .ToString());
          swLog.WriteLine(" possession: " + iPossH      .ToString(        ).PadLeft(9) + " / " + iPossA      .ToString(        ).PadRight(9) + ": " + (iPossH       / (double)iPossA)      .ToString());
          swLog.WriteLine("     passes: " + iPassH      .ToString(        ).PadLeft(9) + " / " + iPassA      .ToString(        ).PadRight(9) + ": " + (iStepsH      / (double)iStepsA)     .ToString());
          swLog.WriteLine("   offsites: " + iOffsiteH   .ToString(        ).PadLeft(9) + " / " + iOffsiteA   .ToString(        ).PadRight(9) + ": " + (iOffsiteH    / (double)iOffsiteA)   .ToString());

          swLog.WriteLine("Avrge goals: " + (iGH / (float)nGames).ToString("0.00") + ":" + (iGA / (float)nGames).ToString("0.00"));
          swLog.WriteLine("Win Home/Draw/Away: " + iV.ToString() + "/" + iD.ToString() + "/" + iL.ToString());
#if _ML
          swLog.WriteLine("fWfPass: " + fWfPass.ToString("0.0000"));
#endif
          swLog.WriteLine("Finish date: " + DateTime.Now);
          swLog.WriteLine();
#endif
          swLog.Close();

#if !_DoE
          Assert.AreEqual(1.0, iGH          / (double)iGA,          0.2);
          Assert.AreEqual(1.0, iShootsH     / (double)iShootsA,     0.2);
          Assert.AreEqual(1.0, fChanceGoalH /         fChanceGoalA, 0.2);
          Assert.AreEqual(1.0, fShootDistH  /         fShootDistA,  0.2);
          Assert.AreEqual(1.0, iDuelH       / (double)iDuelA,       0.2);
          Assert.AreEqual(1.0, iStepsH      / (double)iStepsA,      0.2);
          Assert.AreEqual(1.0, iPossH       / (double)iPossA,       0.2);
          Assert.AreEqual(1.0, iOffsiteH    / (double)iOffsiteA,    0.2);
#endif

#if _DoE
          fWf2 += fWfStep;
        }
        
        fWf1 += fWfStep;
      }
#endif
    }

    [TestMethod]
    public void TestIOGameData()
    {
      CornerkickManager.Main mn = new CornerkickManager.Main();
      mn.sHomeDir = Path.Combine(mn.sHomeDir, "test");
      if (!Directory.Exists(mn.sHomeDir)) Directory.CreateDirectory(mn.sHomeDir);

      CornerkickGame.Game gameTest = game.tl.getDefaultGame();
      for (byte iHA = 0; iHA < 2; iHA++) {
        for (byte iPl = 0; iPl < gameTest.player[iHA].Length; iPl++) mn.ltPlayer.Add(gameTest.player[iHA][iPl]);
      }

      int[] iSeats = new int[3];
      for (byte iS = 0; iS < iSeats.Length; iS++) iSeats[iS] = gameTest.data.stadium.getSeats(iS);
      
      bool bOk = mn.doGame(gameTest, bAlwaysWriteToDisk: true, bWaitUntilGameIsSaved: true);
      Assert.AreEqual(true, bOk);

      DirectoryInfo diGames = new DirectoryInfo(@Path.Combine(mn.sHomeDir, "save", "games")); //Assuming Test is your Folder
      FileInfo[] fiGames = diGames.GetFiles("*.ckgx");
      while (fiGames.Length == 0) {
        fiGames = diGames.GetFiles("*.ckgx");
      }
      foreach(FileInfo fiGame in fiGames)
      {
        CornerkickGame.Game gameLoad = mn.io.loadGame(fiGame.FullName);

        for (byte iS = 0; iS < iSeats.Length; iS++) Assert.AreEqual(iSeats[iS], gameTest.data.stadium.getSeats(iS));
      }      
    }

    private void testHA(CornerkickGame.Game game0)
    {
      CornerkickGame.Game game1 = switchTeams(game0);

      if (game0.ball.plAtBall != null) {
        CornerkickGame.Player plDummy = null;

        CornerkickGame.Player plClosestOpp0 = game0.tl.getClosestPlayer(game0.ball.plAtBall, game0.ball.plAtBall.iHA == 1);
        int iDistClosestOpp0 = game0.tl.getDistancePlayerSteps(game0.ball.plAtBall, plClosestOpp0);
        CornerkickGame.Player plClosestOpp1 = game1.tl.getClosestPlayer(game1.ball.plAtBall, game1.ball.plAtBall.iHA == 1);
        int iDistClosestOpp1 = game1.tl.getDistancePlayerSteps(game1.ball.plAtBall, plClosestOpp1);

#if !_AI2
        // Shoot
        double fShootChance0 = game0.ai.getChanceShoot(game0.ball.plAtBall, game0.data.team[0].tc, game0.ball.nPassSteps > 0, Math.Abs(game0.iStandard) == 2);
        double fShootChance1 = game1.ai.getChanceShoot(game1.ball.plAtBall, game1.data.team[1].tc, game1.ball.nPassSteps > 0, Math.Abs(game1.iStandard) == 2);
        if (fShootChance0 != fShootChance1) {
          fShootChance0 = game0.ai.getChanceShoot(game0.ball.plAtBall, game0.data.team[0].tc, game0.ball.nPassSteps > 0, Math.Abs(game0.iStandard) == 2);
          fShootChance1 = game1.ai.getChanceShoot(game1.ball.plAtBall, game1.data.team[1].tc, game1.ball.nPassSteps > 0, Math.Abs(game1.iStandard) == 2);

          fShootChance0 = game0.ai.getChanceShoot(game0.ball.plAtBall, game0.data.team[0].tc, game0.ball.nPassSteps > 0, Math.Abs(game0.iStandard) == 2);
          fShootChance1 = game1.ai.getChanceShoot(game1.ball.plAtBall, game1.data.team[1].tc, game1.ball.nPassSteps > 0, Math.Abs(game1.iStandard) == 2);

          fShootChance0 = game0.ai.getChanceShoot(game0.ball.plAtBall, game0.data.team[0].tc, game0.ball.nPassSteps > 0, Math.Abs(game0.iStandard) == 2);
          fShootChance1 = game1.ai.getChanceShoot(game1.ball.plAtBall, game1.data.team[1].tc, game1.ball.nPassSteps > 0, Math.Abs(game1.iStandard) == 2);
        }
        Assert.AreEqual(fShootChance0, fShootChance1, 0.0001);
#endif

        // Pass
#if !_AI2
        int iDummy1 = 0;
        int iDummy2 = 0;
        double fPassChance0 = game0.ai.getChancePass(game0.ball.plAtBall, iDistClosestOpp0, game0.data.team[0].tc.fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);
        double fPassChance1 = game1.ai.getChancePass(game1.ball.plAtBall, iDistClosestOpp1, game1.data.team[1].tc.fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);
        if (fPassChance0 != fPassChance1) {
          fPassChance0 = game0.ai.getChancePass(game0.ball.plAtBall, iDistClosestOpp0, game0.data.team[0].tc.fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);
          fPassChance1 = game1.ai.getChancePass(game1.ball.plAtBall, iDistClosestOpp1, game1.data.team[1].tc.fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);

          fPassChance0 = game0.ai.getChancePass(game0.ball.plAtBall, iDistClosestOpp0, game0.data.team[0].tc.fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);
          fPassChance1 = game1.ai.getChancePass(game1.ball.plAtBall, iDistClosestOpp1, game1.data.team[1].tc.fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);

          fPassChance0 = game0.ai.getChancePass(game0.ball.plAtBall, iDistClosestOpp0, game0.data.team[0].tc.fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);
          fPassChance1 = game1.ai.getChancePass(game1.ball.plAtBall, iDistClosestOpp1, game1.data.team[1].tc.fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);
        }
        //Assert.AreEqual(fPassChance0, fPassChance1, 0.0001);

        // Receiver
        List<double> fReceiverChance0;
        List<CornerkickGame.Player> ltReceiver0 = game0.ai.getReceiverList(game0.ball.plAtBall, out fReceiverChance0);

        List<double> fReceiverChance1;
        List<CornerkickGame.Player> ltReceiver1 = game1.ai.getReceiverList(game1.ball.plAtBall, out fReceiverChance1);

        if (ltReceiver0.Count != ltReceiver1.Count) {
          ltReceiver0 = game0.ai.getReceiverList(game0.ball.plAtBall, out fReceiverChance0);
          ltReceiver1 = game1.ai.getReceiverList(game1.ball.plAtBall, out fReceiverChance1);

          ltReceiver0 = game0.ai.getReceiverList(game0.ball.plAtBall, out fReceiverChance0);
          ltReceiver1 = game1.ai.getReceiverList(game1.ball.plAtBall, out fReceiverChance1);

          ltReceiver0 = game0.ai.getReceiverList(game0.ball.plAtBall, out fReceiverChance0);
          ltReceiver1 = game1.ai.getReceiverList(game1.ball.plAtBall, out fReceiverChance1);
        }

        Assert.AreEqual(fReceiverChance0.Count, fReceiverChance1.Count);
        for (int i = 0; i < fReceiverChance0.Count; i++) {
          double f0 = fReceiverChance0[i];
          double f1 = fReceiverChance1[i];

          Assert.AreEqual(false, double.IsNaN(f0));
          Assert.AreEqual(false, double.IsNaN(f1));
          Assert.AreEqual(f0, f1, 0.0001);
        }
#endif

        // Solo
        if (plClosestOpp0 != null && plClosestOpp1 != null) {
#if _AI2
#else
          double fSolo0 = game0.ai.getChanceSolo(game0.tl.getSkillEff(game0.ball.plAtBall, 2), game0.tl.getSkillEff(plClosestOpp0, 3), iDistClosestOpp0, game0.tl.getPosPlayer(game0.ball.plAtBall, plClosestOpp0), game0.ball.plAtBall.iLookAt);
          double fSolo1 = game1.ai.getChanceSolo(game1.tl.getSkillEff(game1.ball.plAtBall, 2), game1.tl.getSkillEff(plClosestOpp1, 3), iDistClosestOpp1, game1.tl.getPosPlayer(game1.ball.plAtBall, plClosestOpp1), game1.ball.plAtBall.iLookAt);
          Assert.AreEqual(fSolo0, fSolo1, 0.0001);
#endif
        }

        // Player action
        float[] fAction0 = game0.ai.getPlayerAction(game0.ball.plAtBall, out plDummy, false, 0);
        float[] fAction1 = game1.ai.getPlayerAction(game1.ball.plAtBall, out plDummy, false, 0);
        for (byte iA = 0; iA < fAction0.Length; iA++) {
          //Assert.AreEqual(fAction0[iA], fAction1[iA], 0.0001);
        }
      }
    }

    private CornerkickGame.Game switchTeams(CornerkickGame.Game game0)
    {
      CornerkickGame.Player[][] pl1 = new CornerkickGame.Player[2][];

      for (byte iHA = 0; iHA < 2; iHA++) {
        pl1[iHA] = new CornerkickGame.Player[game0.data.nPlStart];
        for (byte iP = 0; iP < game0.data.nPlStart; iP++) {
          pl1[iHA][iP] = game0.player[iHA][iP].Clone(true);
        }
      }

      CornerkickGame.Game.Data gd0 = game0.data.Clone(true);
      CornerkickGame.Game game1 = new CornerkickGame.Game(gd0, pl1);
      game1.iStandard = game0.iStandard;
      game1.data.bInjuriesPossible = game0.data.bInjuriesPossible;
      game1.data.bCardsPossible    = game0.data.bCardsPossible;

      for (byte iP = 0; iP < game.data.nPlStart; iP++) {
        // Position
        Point ptPosH = game0.player[0][iP].ptPos;
        game1.player[0][iP].ptPos = game0.tl.transformPosition(game0.player[1][iP].ptPos);
        game1.player[1][iP].ptPos = game0.tl.transformPosition(ptPosH);

        // Look at
        int iLookAt0 = game0.player[0][iP].iLookAt - 3;
        int iLookAt1 = game0.player[1][iP].iLookAt - 3;
        if (iLookAt0 < 0) iLookAt0 += 6;
        if (iLookAt1 < 0) iLookAt1 += 6;
        game1.player[0][iP].iLookAt = (byte)iLookAt1;
        game1.player[1][iP].iLookAt = (byte)iLookAt0;

        // CFM
        float fC = game0.player[0][iP].fCondition;
        game1.player[0][iP].fCondition = game1.player[1][iP].fCondition;
        game1.player[1][iP].fCondition = fC;
        float fF = game0.player[0][iP].fFresh;
        game1.player[0][iP].fFresh = game1.player[1][iP].fFresh;
        game1.player[1][iP].fFresh = fF;
        float fM = game0.player[0][iP].fMoral;
        game1.player[0][iP].fMoral = game1.player[1][iP].fMoral;
        game1.player[1][iP].fMoral = fM;

        // Steps
        float fS = game0.player[0][iP].fSteps;
        game1.player[0][iP].fSteps = game1.player[1][iP].fSteps;
        game1.player[1][iP].fSteps = fS;
      }

      // Ball
      game1.ball.ptPos     = game0.tl.transformPosition(game0.ball.ptPos);
      game1.ball.ptPosLast = game0.tl.transformPosition(game0.ball.ptPosLast);
      if (game0.ball.plAtBall     != null) game1.ball.plAtBall     = game1.player[1 - game0.ball.plAtBall    .iHA][game0.ball.plAtBall    .iIndex];
      if (game0.ball.plAtBallLast != null) game1.ball.plAtBallLast = game1.player[1 - game0.ball.plAtBallLast.iHA][game0.ball.plAtBallLast.iIndex];
      game1.ball.nPassSteps = game0.ball.nPassSteps;

      return game1;
    }

    [TestMethod]
    public void TestGamesParallel()
    {
      const int nGames = 50;

      CornerkickManager.Main mn = new CornerkickManager.Main();
      
      List<CornerkickGame.Game.Data> ltGameData = new List<CornerkickGame.Game.Data>();
      for (int iG = 0; iG < nGames; iG++) {
        CornerkickGame.Game gameTest = game.tl.getDefaultGame(iClubIdStart: mn.ltClubs.Count, iPlIdStart: mn.ltPlayer.Count);

        CornerkickManager.Club clbH = new CornerkickManager.Club();
        CornerkickManager.Club clbA = new CornerkickManager.Club();
        foreach (CornerkickGame.Player pl in gameTest.player[0]) {
          mn.ltPlayer.Add(pl);
          clbH.ltPlayer.Add(pl);
        }
        foreach (CornerkickGame.Player pl in gameTest.player[1]) {
          mn.ltPlayer.Add(pl);
          clbA.ltPlayer.Add(pl);
        }
        mn.ltClubs.Add(clbH);
        mn.ltClubs.Add(clbA);

        ltGameData.Add(gameTest.data);
      }

      bool bOk = mn.doGames(ltGameData, bBackground: true);

      Assert.AreEqual(true, bOk);

      int iGH = 0;
      int iGA = 0;
      int iV = 0;
      int iD = 0;
      int iL = 0;
      foreach (CornerkickGame.Game.Data gd in ltGameData) {
        iGH += gd.team[0].iGoals;
        iGA += gd.team[1].iGoals;

        if      (gd.team[0].iGoals > gd.team[1].iGoals) iV++;
        else if (gd.team[0].iGoals < gd.team[1].iGoals) iL++;
        else                                            iD++;
      }
      Debug.WriteLine("Total: " + iGH.ToString() + ":" + iGA.ToString());
      Debug.WriteLine("Avrge: " + (iGH / (float)nGames).ToString("0.00") + ":" + (iGA / (float)nGames).ToString("0.00"));
      Debug.WriteLine("Win Home/Draw/Away: " + iV.ToString() + "/" + iD.ToString() + "/" + iL.ToString());
    }
    
    [TestMethod]
    public void TestMatchdays()
    {
      const int iLand = 36;

      CornerkickManager.Main mn = new CornerkickManager.Main();

      //mn.setNewSeason();

      /////////////////////////////////////////////////////////////////////
      // Create "internat." cup
      CornerkickManager.Cup cupInter = new CornerkickManager.Cup(bKo: true, bKoTwoGames: true, nGroups: 4, bGroupsTwoGames: true, nQualifierKo: 2);
      cupInter.iId = 3;
      cupInter.sName = "International Cup";
      cupInter.settings.iNeutral = 2;
      cupInter.settings.iBonusCupWin = 30000000; // 30 mio.
      cupInter.settings.bBonusReleaseCupWinInKo = true;
      cupInter.settings.iDayOfWeek = 3;
      mn.ltCups.Add(cupInter);

      /////////////////////////////////////////////////////////////////////
      // Create nat. cup
      CornerkickManager.Cup cup = new CornerkickManager.Cup(bKo: true);
      cup.iId  = 2;
      cup.sName = "National Cup";
      cup.iId2 = iLand;
      mn.ltCups.Add(cup);

      /////////////////////////////////////////////////////////////////////
      // Create league
      CornerkickManager.Cup league = new CornerkickManager.Cup(nGroups: 1, bGroupsTwoGames: true);
      league.iId  = 1;
      league.sName = "League";
      league.iId2 = iLand;
      mn.ltCups.Add(league);

      /////////////////////////////////////////////////////////////////////
      // Create Clubs
      int nTeams = 16;
      for (byte i = 0; i < nTeams; i++) {
        CornerkickManager.Club clb = new CornerkickManager.Club();

        clb.iId = i;
        clb.sName = "Team" + (i + 1).ToString();
        clb.iLand = iLand;
        clb.iDivision = 0;

        addPlayerToClub(mn, ref clb);

        mn.fz.addSponsor(clb, true);

        mn.ltClubs.Add(clb);

        cupInter.ltClubs[i/4].Add(clb);
        cup     .ltClubs[  0].Add(clb);
        league  .ltClubs[  0].Add(clb);

        mn.doFormation(clb.iId);

        // Put last player on transferlist
        mn.ui.putPlayerOnTransferlist(clb.ltPlayer[clb.ltPlayer.Count - 1].iId, 0);
      }

      DateTime dtLeagueStart;
      DateTime dtLeagueEnd;
      mn.setSeasonStartEndDates(out dtLeagueStart, out dtLeagueEnd);

      /////////////////////////////////////////////////////////////////////
      // Create WC
      CornerkickManager.Cup cupWc = new CornerkickManager.Cup(bKo: true, bKoTwoGames: false, nGroups: 2, bGroupsTwoGames: false, nQualifierKo: 2);
      cupWc.iId = 7;
      cupWc.sName = "World Cup";
      cupWc.settings.iNeutral = 1;
      cupWc.settings.dtStart = dtLeagueEnd.Date + new TimeSpan(20, 30, 00);
      cupWc.settings.dtEnd   = mn.dtSeasonEnd.AddDays(-1).Date + new TimeSpan(20, 00, 00);
      mn.ltCups.Add(cupWc);

      byte[] iNations = new byte[8] { 3, 13, 29, 30, 33, 36, 45, 54 };
      int iGroup = 0;
      foreach (byte iN in iNations) {
        CornerkickManager.Club clbNat = new CornerkickManager.Club();
        clbNat.bNation = true;
        clbNat.iId = mn.ltClubs.Count;
        clbNat.sName = mn.sLand[iN];
        clbNat.iLand = iN;
        clbNat.formation = mn.ltFormationen[8];

        List<CornerkickGame.Player> ltPlayerBest = mn.getBestPlayer(iN, clbNat.formation);
        while ((ltPlayerBest = mn.getBestPlayer(iN, clbNat.formation)).Count < 22) mn.plr.newPlayer(iNat: iN);
        //Assert.AreEqual(true, ltPlayerBest.Count >= 11);
        //clbNat.ltPlayer = ltPlayerBest;

        mn.ltClubs.Add(clbNat);

        mn.doFormation(clbNat.iId);

        cupWc.ltClubs[iGroup / 4].Add(clbNat);
        iGroup++;
      }

      cupInter.settings.dtStart = dtLeagueStart.AddDays((int)((dtLeagueEnd - dtLeagueStart).TotalDays / 4.0)).Date + new TimeSpan(20, 45, 00);

      mn.calcMatchdays();

      mn.drawCup(cupInter);
      mn.drawCup(league);
      mn.drawCup(cupWc);
      
      // Perform next step until end of season
      while (mn.next() < 99) {
        Debug.WriteLine(mn.dtDatum.ToString());
      }

      Debug.WriteLine("Start of season: " + mn.dtSeasonStart.ToString());
      foreach (CornerkickManager.Cup cupTmp in mn.ltCups) {
        Debug.WriteLine(cupTmp.sName);
        
        int iMd = 1;
        foreach (CornerkickManager.Cup.Matchday md in cupTmp.ltMatchdays) {
          if (md.ltGameData == null) break;

          Debug.WriteLine(iMd++.ToString().PadLeft(2) + " - " + md.dt.ToString());
          foreach (CornerkickGame.Game.Data gd in md.ltGameData) {
            string sGame = ("Team_" + gd.team[0].iTeamId.ToString()).PadLeft(7) + " - " + ("Team_" + gd.team[1].iTeamId.ToString()).PadLeft(7);
            string sResult = mn.ui.getResultString(gd);
            if (!string.IsNullOrEmpty(sResult)) sGame += " - " + sResult;
            Debug.WriteLine("  " + sGame);
          }
        }
      }
      Debug.WriteLine("End of season: " + mn.dtSeasonEnd.ToString());

      // League
      List<CornerkickManager.Tool.TableItem> table = mn.tl.getLeagueTable(league);

      Assert.AreEqual(true, table.Count == nTeams);
      Assert.AreEqual(true, table[0].iGoals > 0);
      Assert.AreEqual(true, table[0].iGUV[0] > table[0].iGUV[2]);
      
      Assert.AreEqual(true, cup.ltMatchdays.Count > 3);
      Assert.AreEqual(true, cup.ltMatchdays[3].ltGameData.Count == 1);

      Assert.AreEqual(true, cupInter.ltMatchdays.Count > 10);
      Assert.AreEqual(true, cupInter.ltMatchdays[10].ltGameData.Count == 1);

      Assert.AreEqual(true, cupWc.ltMatchdays.Count > 4);
      Assert.AreEqual(true, cupWc.ltMatchdays[4].ltGameData.Count == 1);

      // save/load
#if _ANSYS
      string sSaveFile = "F:\\test\\unittest_save";
#else
      string sSaveFile = "unittest_save";
#endif

      DateTime dtCurrent = mn.dtDatum;
      int iSeasonCountTmp = mn.iSaisonCount;
      float fInterest = mn.fz.fGlobalCreditInterest;

      List<string> ltUserId = new List<string>();
      foreach (CornerkickManager.User user in mn.ltUser) {
        ltUserId.Add(user.id);
      }

      List<string> ltPlayerName = new List<string>();
      foreach (CornerkickGame.Player pl in mn.ltPlayer) {
        ltPlayerName.Add(pl.sName);
      }

      mn.io.save(sSaveFile);
      mn.io.load(sSaveFile);

      // General
      Assert.AreEqual(dtCurrent, mn.dtDatum);
      Assert.AreEqual(iSeasonCountTmp, mn.iSaisonCount);
      Assert.AreEqual(fInterest, mn.fz.fGlobalCreditInterest);

      // User id
      Assert.AreEqual(ltUserId.Count, mn.ltUser.Count);
      for (int iU = 0; iU < ltUserId.Count; iU++) {
        Assert.AreEqual(ltUserId[iU], mn.ltUser[iU].id);
      }

      // Player name
      Assert.AreEqual(ltPlayerName.Count, mn.ltPlayer.Count);
      for (int iPl = 0; iPl < ltPlayerName.Count; iPl++) {
        Assert.AreEqual(ltPlayerName[iPl], mn.ltPlayer[iPl].sName);
      }
    }

    /*
    [TestMethod]
    public void TestSeason()
    {
      CornerkickManager.Main cr = new CornerkickManager.Main();

      mn.ltLiga[0].Add(new List<int>());

      int nTeams = 18;
      for (byte i = 0; i < nTeams; i++) {
        CornerkickManager.Club clb = mn.ini.newClub();

        clb.iId = i;
        clb.sName = "Team" + (i + 1).ToString();
        clb.iLand = 0;
        clb.iDivision = 0;
        clb.iPokalrunde = 0;

        addPlayerToClub(cr, ref clb);

        mn.ltClubs.Add(clb);

        mn.ltLiga[clb.iLand][clb.iDivision].Add(clb.iId);

        mn.doFormationKI(clb.iId, true);
      }

      mn.calcSpieltage();
      
      mn.setNewSeason();

      // Perform next step until end of season
      while (mn.next() < 99) ;

      // League
      List<CornerkickManager.Tool.TableItem> table = mn.tl.getLeagueTable(1, 0, 0);

      Assert.AreEqual(true, table.Count == nTeams);
      Assert.AreEqual(true, table[0].iGoals > 0);
      Assert.AreEqual(true, table[0].iGUV[0] > table[0].iGUV[2]);
      
      // Cup
      CornerkickManager.Main.Cup cup = mn.tl.getCup(0, 2);
      Assert.AreEqual(true, cup.ltMatchdays.Count > 3);
      Assert.AreEqual(true, cup.ltMatchdays[3].ltGameData.Count == 1);
    }
    */

    [TestMethod]
    public void TestTrainingCamp()
    {
      float[] fCondi = new float[2];
      float[] fFresh = new float[2];
      float[] fMood  = new float[2];

      for (byte i = 0; i < 2; i++) {
        CornerkickManager.Main mn  = new CornerkickManager.Main();
        mn.dtDatum = mn.dtDatum.AddDays(1);

        CornerkickManager.User usr = new CornerkickManager.User();
        usr.iTeam = 0;
        mn.ltUser.Add(usr);

        // Create Club
        CornerkickManager.Club clb = new CornerkickManager.Club();
        clb.iId = 0;
        clb.sName = "Team";
        clb.iLand = 0;
        clb.iDivision = 0;
        clb.iUser = 0;
        clb.training.iTraining[1] = 2; // Condition
        mn.ltClubs.Add(clb);

        CornerkickGame.Player pl = new CornerkickGame.Player();
        pl.fCondition = 0.8f;
        pl.fFresh     = 0.8f;
        pl.fMoral     = 0.8f;
        pl.iClubId = 0;
        pl.dtBirthday = mn.dtDatum.AddYears(-25);
        clb.ltPlayer.Add(pl);

        if (i > 0) {
          CornerkickManager.TrainingCamp.Camp cmp = mn.tcp.ltCamps[1];
          mn.tcp.bookCamp(ref clb, cmp, mn.dtDatum.AddDays(-1), mn.dtDatum.AddDays(+1));
        }

        CornerkickManager.TrainingCamp.Booking tcb = mn.tcp.getCurrentCamp(clb, mn.dtDatum);

        mn.plr.doTraining(ref pl, mn.dtDatum, tcb);

        fCondi[i] = pl.fCondition;
        fFresh[i] = pl.fFresh;
        fMood [i] = pl.fMoral;
      }

      Assert.AreEqual(1.0101672, fCondi[1] / fCondi[0], 0.00001);
      Assert.AreEqual(1.0005339, fFresh[1] / fFresh[0], 0.00001);
      Assert.AreEqual(1.0003653, fMood [1] / fMood [0], 0.00001);
    }

    void addPlayerToClub(CornerkickManager.Main mn, ref CornerkickManager.Club clb)
    {
      for (byte i = 0; i < 2; i++) {
        for (byte iP = 1; iP < 12; iP++) {
          CornerkickGame.Player pl = mn.plr.newPlayer(clb, iP);
          pl.iNr = (byte)(iP * (i + 1));
        }
      }
    }

    void testGoal(CornerkickGame.Game gameTest, bool bHome, int iGoalH, int iGoalA)
    {
      if (bHome) testGoal(gameTest, 0, iGoalH, iGoalA);
      else       testGoal(gameTest, 1, iGoalH, iGoalA);
    }
    void testGoal(CornerkickGame.Game gameTest, byte iHA, int iGoalH, int iGoalA)
    {
      if (iHA == 0) {
        if (iGoalH + 1 != gameTest.data.team[0].iGoals) {
          while (gameTest.iStandardCounter > 0) {
            gameTest.next();
          }
          gameTest.next();
          gameTest.next();
        }
        Assert.AreEqual(iGoalH + 1, gameTest.data.team[0].iGoals);
        Assert.AreEqual(iGoalA,     gameTest.data.team[1].iGoals);
      } else {
        if (iGoalA + 1 != gameTest.data.team[1].iGoals) {
          while (gameTest.iStandardCounter > 0) {
            gameTest.next();
          }
          gameTest.next();
          gameTest.next();
        }
        Assert.AreEqual(iGoalH,     gameTest.data.team[0].iGoals);
        Assert.AreEqual(iGoalA + 1, gameTest.data.team[1].iGoals);
      }

      if (iHA == 0) Assert.AreEqual(gameTest.ptPitch.X + 1, gameTest.ball.ptPos.X, "Ball is not in away goal!");
      else          Assert.AreEqual(                   - 1, gameTest.ball.ptPos.X, "Ball is not in home goal!");
      Assert.AreEqual(5, Math.Abs(gameTest.iStandard), "Standard is not kick-off!");
        
      gameTest.next();
      while (gameTest.iStandardCounter > 0) {
        Assert.AreEqual(gameTest.ptPitch.X / 2, gameTest.ball.ptPos.X, "Ball is not in middle-point!");
        Assert.AreEqual(                     0, gameTest.ball.ptPos.Y, "Ball is not in middle-point!");
        gameTest.next();
      }
    }

  }
}
