﻿//#define _ML
#define _AI2
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
  public class UnitTests
  {
    CornerkickGame.Game game = new CornerkickGame.Game(new CornerkickGame.Game.Data());

    [TestMethod]
    public void TestDistanceSteps()
    {
      Assert.AreEqual(3, CornerkickGame.Tool.getDistanceSteps(                  7,  0,                  1, 0));
      Assert.AreEqual(3, CornerkickGame.Tool.getDistanceSteps(game.ptPitch.X -  7,  0, game.ptPitch.X - 1, 0));

      Assert.AreEqual(3, CornerkickGame.Tool.getDistanceSteps(                  4,  3,                  1, 0));
      Assert.AreEqual(3, CornerkickGame.Tool.getDistanceSteps(game.ptPitch.X -  4,  3, game.ptPitch.X - 1, 0));

      Assert.AreEqual(6, CornerkickGame.Tool.getDistanceSteps(                 10,  3,                  1, 0));
      Assert.AreEqual(6, CornerkickGame.Tool.getDistanceSteps(game.ptPitch.X - 10,  3, game.ptPitch.X - 1, 0));

      Assert.AreEqual(6, CornerkickGame.Tool.getDistanceSteps(                 10, -3,                  1, 0));
      Assert.AreEqual(6, CornerkickGame.Tool.getDistanceSteps(game.ptPitch.X - 10, -3, game.ptPitch.X - 1, 0));

      Assert.AreEqual(4, CornerkickGame.Tool.getDistanceSteps(                  1,  4,                  1, 0));
      Assert.AreEqual(4, CornerkickGame.Tool.getDistanceSteps(game.ptPitch.X -  1,  4, game.ptPitch.X - 1, 0));
    }

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
        else plOffsite.ptPos.X--;
        Assert.AreEqual(true, gameTest.tl.checkPlayerIsOffsite(plOffsite), "Player is not offsite!");
      }
    }

    [TestMethod]
    public void TestPlayerOutsidePitch()
    {
      CornerkickGame.Game gameTest = game.tl.getDefaultGame();
      gameTest.next();
      while (gameTest.iStandard != 0) gameTest.next();

      CornerkickGame.Player pl = gameTest.player[0][0];

      // not offsite
      pl.ptPos = new Point(-3, 0);

      gameTest.next();
      gameTest.next();

      Assert.AreEqual(true, pl.ptPos.X >= 0, "Player is outside pitch: pl.X=" + pl.ptPos.X);
    }

    [TestMethod]
    public void TestShoot()
    {
      string[] sHA = { "Home", "Away" };
      for (byte iHA = 0; iHA < 2; iHA++) {
        CornerkickGame.Game gameTest = game.tl.getDefaultGame();
        gameTest.next();

        CornerkickGame.Player plShoot = gameTest.player[iHA][10];
        CornerkickGame.Player plKeeper = gameTest.tl.getKeeper(iHA == 1);

        // chance
        resetPlayerShoot(plShoot, plKeeper, gameTest, iHA);
        float[] fDist = CornerkickGame.Tool.getDistanceToGoal(plShoot.ptPos, iHA == 0, gameTest.ptPitch.X, gameTest.fConvertDist2Meter);
        Assert.AreEqual(0.52828, CornerkickGame.AI.getChanceShootOnGoal(7f, fDist, iHA == 0, plShoot.iLookAt), 0.0001, "ChanceShootOnGoal");

        float[] fKeeper = gameTest.getKeeperSkills(plKeeper, plShoot);
        float[] fShoot = gameTest.getShootSkills(plShoot);
        // 76.3 %
        Assert.AreEqual(0.7558016777038574, CornerkickGame.AI.getChanceShootKeeperSave(fKeeper, fDist, fShoot), 0.0001, "ChanceKeeperSave");

        // aside (0)
        resetPlayerShoot(plShoot, plKeeper, gameTest, iHA);
        gameTest.doShoot(plShoot, 0, 0);
        while (gameTest.shootInProgress != null) gameTest.next();
        if (iHA == 0) Assert.AreEqual(true, gameTest.ball.ptPos.X > gameTest.ptPitch.X, "Ball is not away out!");
        else Assert.AreEqual(true, gameTest.ball.ptPos.X < gameTest.ptPitch.X, "Ball is not home out!");
        gameTest.next();
        Assert.AreEqual(6, Math.Abs(gameTest.iStandard), "Standard is not Goal-off!");

        // goal (1)
        resetPlayerShoot(plShoot, plKeeper, gameTest, iHA);
        int iGoalH = gameTest.data.team[0].iGoals;
        int iGoalA = gameTest.data.team[1].iGoals;
        gameTest.doShoot(plShoot, 0, 1);
        while (gameTest.shootInProgress != null) gameTest.next();
        Utility.testGoal(gameTest, iHA, iGoalH, iGoalA);

        // save (2)
        resetPlayerShoot(plShoot, plKeeper, gameTest, iHA);
        gameTest.doShoot(plShoot, 0, 2);
        while (gameTest.shootInProgress != null) gameTest.next();
        Assert.AreEqual(plKeeper.ptPos.X, gameTest.ball.ptPos.X, "Ball is not at away keeper!");
        Assert.AreEqual(plKeeper.ptPos.Y, gameTest.ball.ptPos.Y, "Ball is not at home keeper!");

        // cornerkick (4)
        resetPlayerShoot(plShoot, plKeeper, gameTest, iHA);
        gameTest.doShoot(plShoot, 0, 4);
        while (gameTest.shootInProgress != null) gameTest.next();
        if (iHA == 0) Assert.AreEqual(true, gameTest.ball.ptPos.X > gameTest.ptPitch.X + 2, "Ball is not away out!");
        else Assert.AreEqual(true, gameTest.ball.ptPos.X < -2, "Ball is not home out!");
        gameTest.next();
        Assert.AreEqual(3, Math.Abs(gameTest.iStandard), "Standard is not cornerkick!");

        //for (int iS = gameTest.iStandardCounter; iS >= 0; iS--) {
        while (gameTest.iStandardCounter > 0) {
          //Assert.AreEqual(gameTest.iStandardCounter, iS, "StandardCounter is not " + iS.ToString() + " but " + gameTest.iStandardCounter.ToString() + "!");

          Assert.AreEqual(gameTest.ptPitch.X * (1 - iHA), gameTest.ball.ptPos.X, "Ball X is not at " + sHA[1 - iHA] + " corner!");
          Assert.AreEqual(gameTest.ptPitch.Y, Math.Abs(gameTest.ball.ptPos.Y), "Ball Y is not at " + sHA[1 - iHA] + " corner!");

          gameTest.next();
        }
      }
    }
    private void resetPlayerShoot(CornerkickGame.Player plShoot, CornerkickGame.Player plKeeper, CornerkickGame.Game gameTest, byte iHA)
    {
      plShoot.fMoral = 1f;
      plShoot.fFresh = 1f;
      plShoot.ptPos.X = (int)Math.Round((gameTest.ptPitch.X * 0.8) - (iHA * gameTest.ptPitch.X * 0.6));
      plShoot.ptPos.Y = (iHA * 2) - 1;
      plShoot.fSteps = 7f;
      //plShoot.fStepsAll = plShoot.fSteps;
      plShoot.iLookAt = (byte)(3 - (iHA * 3));

      plKeeper.fMoral = 1f;
      plKeeper.fFresh = 1f;
      plKeeper.fSteps = 7f;
      //plKeeper.fStepsAll = plKeeper.fSteps;
      gameTest.ball.ptPos = plShoot.ptPos;
      gameTest.ball.plAtBall = plShoot;
      gameTest.ball.plAtBallLast = plShoot;
      gameTest.iStandard = 0;
    }

    [TestMethod]
    public void TestShootUnderPassAngle()
    {
      CornerkickGame.Game gameTest = game.tl.getDefaultGame();
      gameTest.next();

      CornerkickGame.Player plShoot = gameTest.player[1][10];
      CornerkickGame.Player plKeeper = gameTest.tl.getKeeper(true);

      // chance
      resetPlayerShoot(plShoot, plKeeper, gameTest, 1);

      plShoot.ptPos.X = 13;
      plShoot.ptPos.Y = 0;

      float[] fShootSkills;

      fShootSkills = gameTest.getShootSkills(plShoot, 0);
      Assert.AreEqual(7.0, fShootSkills[0], 0.00001);

      fShootSkills = gameTest.getShootSkills(plShoot, 0, ptPassOrigin: new Point(13, 13));
      Assert.AreEqual(7.0, fShootSkills[0], 0.00001);

      fShootSkills = gameTest.getShootSkills(plShoot, 0, ptPassOrigin: new Point(1, 0));
      Assert.AreEqual(10.5, fShootSkills[0], 0.00001);

      fShootSkills = gameTest.getShootSkills(plShoot, 0, ptPassOrigin: new Point(0, 13));
      Assert.AreEqual(8.75, fShootSkills[0], 0.00001);

      fShootSkills = gameTest.getShootSkills(plShoot, 0, ptPassOrigin: new Point(26, 13));
      Assert.AreEqual(5.25, fShootSkills[0], 0.00001);

      fShootSkills = gameTest.getShootSkills(plShoot, 0, ptPassOrigin: new Point(26, 0));
      Assert.AreEqual(3.5, fShootSkills[0], 0.00001);
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

        gameTest.doPass(plPass, new Point(gameTest.ptPitch.X * (1 - iHA), plPass.ptPos.Y), false);
        gameTest.ball.iPassStep = 2;
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

        gameTest.doPass(plPass, new Point(plPass.ptPos.X, gameTest.ptPitch.Y * ((2 * iHA) - 1)), false);
        gameTest.ball.iPassStep = 2;
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

        List<CornerkickGame.AI.Receiver> ltReceiver = gameTest.ai.getReceiverList(gameTest.ball.plAtBall, 0);

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

#if !_AI2
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
#endif

        //CornerkickGame.Player plRec = gameTest.ai.getReceiver(plAtBall);
      }
    }

    [TestMethod]
    public void TestPassMaxLength()
    {
      float[] fMaxPassLengthRel = new float[] { 0.43544f, 0.45531f, 0.47222f, 0.48701f, 0.50019f, 0.51211f };
      CornerkickGame.Game gameTest = game.tl.getDefaultGame();

      gameTest.doStart();

      double fDistPitchX = CornerkickGame.Tool.getDistance(new Point(0, 0), new Point(gameTest.ptPitch.X, 0), gameTest.fConvertDist2Meter);
      for (int iS = 4; iS < 10; iS++) {
        double fMaxPassLength = CornerkickGame.Tool.getMaxPassLength(iS);
        Debug.WriteLine(iS.ToString() + ": " + fMaxPassLength / fDistPitchX);
        Assert.AreEqual(fMaxPassLengthRel[iS - 4], fMaxPassLength / fDistPitchX, 0.01);
      }
    }

    [TestMethod]
    public void TestPassAhead()
    {
      CornerkickGame.Game gameTest = game.tl.getDefaultGame(nPlStart: 3);
      double fPassLength;
      int iPassAhead;

      int[] iDistOppY = new int[] { 2, 4, 6, 8, 10, 16, 22 };
      int[] iPassAheadExp = new int[] { 2, 3, 3, 4, 5, 7, 9 };
      //int[] iDistOppY     = new int[] { 8 };
      //int[] iPassAheadExp = new int[] { 4 };
      for (int i = 0; i < iDistOppY.Length; i++) {
        int iDistPlayer = 10;

        for (int iHA = 0; iHA < 2; iHA++) {
          int iDistPlayerOpp = iDistPlayer - 8;

          if (iHA > 0) {
            iDistPlayer *= -1;
            iDistPlayerOpp *= -1;
            iPassAheadExp[i] *= -1;
          }
          gameTest.player[iHA][1].ptPos = new Point(gameTest.ptPitch.X / 2, 0);
          gameTest.player[iHA][2].ptPos = new Point(gameTest.player[iHA][1].ptPos.X + iDistPlayer, 0);

          gameTest.player[1 - iHA][1].ptPos = new Point(gameTest.ptPitch.X / 2, 25);
          gameTest.player[1 - iHA][2].ptPos = new Point(gameTest.player[1 - iHA][1].ptPos.X + iDistPlayerOpp, iDistOppY[i]);

          iPassAhead = gameTest.getPassAhead(gameTest.player[iHA][1], gameTest.player[iHA][2], 0f, 40f, out fPassLength);
          Assert.AreEqual(iPassAheadExp[i], iPassAhead, "Y-dist. opp. player: " + iDistOppY[i].ToString());
        }
      }
    }

    //[TestMethod]
    public void TestStealLowPass()
    {
      CornerkickGame.Game gameTest = game.tl.getDefaultGame(nPlStart: 3);
      gameTest.next();
      while (gameTest.iStandard != 0) gameTest.next();

      gameTest.player[0][1].ptPos = new Point(31, 2);
      gameTest.player[0][2].ptPos = new Point(75, 6);
      gameTest.player[1][1].ptPos = new Point(53, 4);
      gameTest.ball.plAtBall = gameTest.player[0][1];
      gameTest.ball.ptPos = gameTest.player[0][1].ptPos;

      gameTest.doPass(gameTest.player[0][1], gameTest.player[0][2], 0, true);
      //gameTest.next(iPlayerNextAction: 1, iPassNextActionX: gameTest.player[0][2].ptPos.X, iPassNextActionY: gameTest.player[0][2].ptPos.Y);

      while (gameTest.ball.iPassStep > 0) {
        gameTest.next();
      }
      //float fDist = gameTest.tl.getShortestDistance(pt0, pt1, ptA, out ptF);

      //Assert.AreEqual(0.9701425, fDist, 0.00001);
    }

    [TestMethod]
    public void TestDuelFreekick()
    {
      for (byte iHA = 0; iHA < 2; iHA++) {
        CornerkickGame.Game gameTest = game.tl.getDefaultGame();

        gameTest.next();
        while (gameTest.iStandardCounter > 0) gameTest.next();

        CornerkickGame.Player plDef = gameTest.player[iHA][1];
        CornerkickGame.Player plOff = gameTest.player[1 - iHA][10];

        if (iHA == 0) plOff.ptPos = new Point(7, 17);
        gameTest.ball.ptPos = plOff.ptPos;
        gameTest.ball.plAtBall = plOff;
        plDef.ptPos.X = plOff.ptPos.X - (2 - (iHA * 4));

        gameTest.doDuel(plDef, 3);

        Assert.AreEqual(2, Math.Abs(gameTest.iStandard), "Standard is not freekick!");

        while (Math.Abs(gameTest.iStandard) == 2) {
          if (iHA == 0) Assert.AreEqual(1, gameTest.player[iHA][0].ptPos.X);
          else Assert.AreEqual(gameTest.ptPitch.X - 1, gameTest.player[iHA][0].ptPos.X);
          gameTest.next();
        }

        gameTest.next();
      }
    }

    [TestMethod]
    public void TestDuelPenalty()
    {
      for (byte iHA = 0; iHA < 2; iHA++) {
        CornerkickGame.Game gameTest = game.tl.getDefaultGame();

        gameTest.next();
        while (gameTest.iStandardCounter > 0) gameTest.next();

        CornerkickGame.Player plDef = gameTest.player[iHA][1];
        CornerkickGame.Player plOff = gameTest.player[1 - iHA][10];

        plOff.ptPos = new Point(gameTest.ptPitch.X * iHA, 9);
        gameTest.ball.ptPos = plOff.ptPos;
        gameTest.ball.plAtBall = plOff;
        plDef.ptPos.X = plOff.ptPos.X - (2 - (iHA * 4));

        gameTest.doDuel(plDef, 3);

        Assert.AreEqual(1, Math.Abs(gameTest.iStandard), "Standard is not penalty!");

        while (Math.Abs(gameTest.iStandard) == 1) {
          gameTest.next();
        }
      }
    }

    [TestMethod]
    public void TestDuelLastManRed()
    {
      for (byte iHA = 0; iHA < 2; iHA++) {
        CornerkickGame.Game gameTest = game.tl.getDefaultGame();

        gameTest.next();
        while (gameTest.iStandardCounter > 0) gameTest.next();

        CornerkickGame.Player plDef = gameTest.player[iHA][1];
        CornerkickGame.Player plOff = gameTest.player[1 - iHA][10];

        plOff.ptPos = new Point(gameTest.ptBox.X + 4, -7 * (1 - (iHA * 2)));
        if (iHA > 0) plOff.ptPos.X = gameTest.ptPitch.X - plOff.ptPos.X;
        gameTest.ball.ptPos = plOff.ptPos;
        gameTest.ball.plAtBall = plOff;
        plDef.ptPos = new Point(plOff.ptPos.X - (2 - (iHA * 4)), plOff.ptPos.Y);
        for (int i = 2; i < gameTest.player[iHA].Length; i++) {
          if (iHA == 0) {
            while (gameTest.player[iHA][i].ptPos.X <= plDef.ptPos.X) gameTest.player[iHA][i].ptPos.X += 2;
          } else {
            while (gameTest.player[iHA][i].ptPos.X >= plDef.ptPos.X) gameTest.player[iHA][i].ptPos.X -= 2;
          }
        }

        /*
        gameTest.doDuel(plDef, iDuelResult: 3);

        Assert.AreEqual(2, Math.Abs(gameTest.iStandard), "Standard is not freekick!");
        Assert.AreEqual(true, gameTest.state.ltComment != null && gameTest.state.ltComment.Count > 0, "No comment present!");
        Assert.AreEqual(true, !string.IsNullOrEmpty(gameTest.state.ltComment.Find(c => c.sText.Contains("als letzter Mann")).sText), "No last man comment present!");
        */

        // Put defender at same X and test again
        gameTest.iStandard = 0;
        plDef.bYellowCard = false;
        gameTest.state.ltComment.Clear();
        gameTest.player[iHA][2].ptPos = new Point(plDef.ptPos.X + 2 * (1 - (iHA * 2)), 0);

        gameTest.doDuel(plDef, iDuelResult: 3);

        Assert.AreEqual(2, Math.Abs(gameTest.iStandard), "Standard is not freekick!");
        Assert.AreEqual(true, gameTest.state.ltComment != null && gameTest.state.ltComment.Count > 0, "No comment present!");
        //Assert.AreEqual(true, !string.IsNullOrEmpty(gameTest.state.ltComment.Find(c => c.sText.Contains("Glück")).sText), "No last man comment present!");

        while (Math.Abs(gameTest.iStandard) == 2) {
          if (iHA == 0) Assert.AreEqual(1, gameTest.player[iHA][0].ptPos.X);
          else          Assert.AreEqual(gameTest.ptPitch.X - 1, gameTest.player[iHA][0].ptPos.X);
          gameTest.next();
        }

        gameTest.next();
      }
    }

    [TestMethod]
    public void TestDuelStepReduction()
    {
      byte iSkillDuelDef = 8;
      byte iSkillDuelOff = 8;
      float fTacticAggr = 0f;
      float fDuel;

      fDuel = CornerkickGame.AI.getChanceDuelWin(iSkillDuelDef, iSkillDuelOff, fTacticAggr, iTackleSector: 0);
      Assert.AreEqual(1.40625f, CornerkickGame.AI.getDuelStepReduction(fDuel, rndDuel: 0.5), 0.001f);

      fDuel = CornerkickGame.AI.getChanceDuelWin(iSkillDuelDef, iSkillDuelOff, fTacticAggr, iTackleSector: 1);
      Assert.AreEqual(1.11111f, CornerkickGame.AI.getDuelStepReduction(fDuel, rndDuel: 0.5), 0.001f);

      fDuel = CornerkickGame.AI.getChanceDuelWin(iSkillDuelDef, iSkillDuelOff, fTacticAggr, iTackleSector: 2);
      Assert.AreEqual(0.560942, CornerkickGame.AI.getDuelStepReduction(fDuel, rndDuel: 0.5), 0.001f);

      fDuel = CornerkickGame.AI.getChanceDuelWin(iSkillDuelDef, iSkillDuelOff, fTacticAggr, iTackleSector: 3);
      Assert.AreEqual(0.35156f, CornerkickGame.AI.getDuelStepReduction(fDuel, rndDuel: 0.5), 0.001f);
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

      CornerkickGame.Player plDef = gameTest.player[1][1];
      CornerkickGame.Player plOff1 = gameTest.player[0][8];
      CornerkickGame.Player plOff2 = gameTest.player[0][9];
      CornerkickGame.Player plOff3 = gameTest.player[0][10];

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

        float[] fPlAction;
        sbyte iAction = gameTest.ai.getPlayerAction(plOff2, out fPlAction, false, 10);
        //Assert.AreEqual(fChance[iRelDist], fPlAction[0], 0.002, "ChanceShoot");
      }
    }

    [TestMethod]
    public void TestChancesFreekick()
    {
      for (int j = 0; j < 100; j++) {
        float[] fDistRel = new float[] { 1.5f, 1.3f, 1.1f };
        //double[] fChanceFkShoot = new double[] { 0.2523913085460663, 0.3319043219089508, 0.42532700300216675 };
        double[] fChanceFkShoot = new double[] { 0.3647512197494507, 0.43172943592071533, 0.5068542957305908 };
        for (int i = 0; i < fDistRel.Length; i++) {
          CornerkickGame.Game gameTest = game.tl.getDefaultGame();

          gameTest.next();
          while (gameTest.iStandardCounter > 0) gameTest.next();

          gameTest.ball.ptPos = new Point((int)(gameTest.ptBox.X * fDistRel[i]), 0);
          gameTest.iStandard = -2;
          gameTest.iStandardCounter = 1;

          gameTest.doFreekick(false);

          Assert.AreEqual(true, gameTest.ball.plAtBall != null);

          float[] fPlActionFreekick;
          gameTest.ai.getPlayerAction(gameTest.ball.plAtBall, out fPlActionFreekick, iReceiverIx: 10);
          Assert.AreEqual(fChanceFkShoot[i], fPlActionFreekick[0], 0.0001, "ChanceShoot Freekick at dist = " + CornerkickGame.Tool.getDistanceToGoal(gameTest.ball.ptPos, false, gameTest.ptPitch.X, gameTest.fConvertDist2Meter)[0].ToString("0.0") + "m Box");
          gameTest.next();
        }
      }
    }

    [TestMethod]
    public void TestChancePassFromSide()
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

      CornerkickGame.Player plDef1 = gameTest.player[1][1];
      CornerkickGame.Player plDef2 = gameTest.player[1][2];
      CornerkickGame.Player plOff1 = gameTest.player[0][8];
      CornerkickGame.Player plOff2 = gameTest.player[0][9];
      CornerkickGame.Player plOff3 = gameTest.player[0][10];

      plOff1.iLookAt = 3;
      plOff2.iLookAt = 3;
      plOff3.iLookAt = 3;

      plDef1.ptPos.Y = 0;
      plDef1.iLookAt = 0;

      float fRelDistX = 0.95f;

      plOff1.ptPos = new Point((int)(gameTest.ptPitch.X * fRelDistX), (int)(gameTest.ptPitch.Y * +0.9));
      plOff2.ptPos = new Point((int)(gameTest.ptPitch.X * fRelDistX), (int)(gameTest.ptPitch.Y * +0.1));
      plOff3.ptPos = new Point((int)(gameTest.ptPitch.X * fRelDistX), (int)(gameTest.ptPitch.Y * -0.1));

      plDef1.ptPos = new Point((int)(gameTest.ptPitch.X * fRelDistX), (int)(gameTest.ptPitch.Y * +0.85));
      plDef2.ptPos = new Point((int)(gameTest.ptPitch.X * fRelDistX), (int)(gameTest.ptPitch.Y * 0.00));

      gameTest.ball.ptPos = plOff1.ptPos;
      gameTest.ball.plAtBall = plOff1;

      float[] fPlAction;
      sbyte iAction = gameTest.ai.getPlayerAction(plOff1, out fPlAction, false, 10);
#if _AI2
      Assert.AreEqual(0.97, fPlAction[1], 0.02);
#else
      Assert.AreEqual(0.7, fPlAction[1], 0.02);
#endif
    }

    [TestMethod]
    public void TestChanceKeeperSave()
    {
      int[] iPosX = new int[] { 25, 31, 37, 81 }; // 20m, 25m, 30m, 60m
      double[] fExp = new double[] { 0.7863984704017639, 0.9044795036315918, 0.9701999425888062, 1.0 };

      for (int i = 0; i < iPosX.Length; i++) {
        CornerkickGame.Game gameTest = game.tl.getDefaultGame();

        gameTest.next();
        while (gameTest.iStandardCounter > 0) gameTest.next();

        // Set all player far away
        for (byte iHA = 0; iHA < 2; iHA++) {
          for (byte iP = 1; iP < gameTest.data.nPlStart; iP++) gameTest.player[iHA][iP].ptPos = new Point(gameTest.ptPitch.X, 0);
        }

        CornerkickGame.Player plShooter = gameTest.player[1][10];

        // Test specific positions
        plShooter.ptPos = new Point(iPosX[i], 0);
        Assert.AreEqual(fExp[i], gameTest.ai.getChanceShootKeeperSave(plShooter, 0), 0.0001, "Distance to goal: " + gameTest.tl.getDistanceToGoal(plShooter)[0].ToString("0.0m"));
      }
    }

    [TestMethod]
    public void TestPhi()
    {
      int iX0 = game.ptPitch.X / 2;
      int iY0 = 0;
      Point pt0 = new Point(iX0, iY0);
      Assert.AreEqual(0.0, CornerkickGame.Tool.getPhi(pt0, new Point(pt0.X - 1, pt0.Y)), 0.0001); // A
      Assert.AreEqual(60.0, CornerkickGame.Tool.getPhi(pt0, new Point(pt0.X - 1, pt0.Y - 1)), 0.0001); // W
      Assert.AreEqual(120.0, CornerkickGame.Tool.getPhi(pt0, new Point(pt0.X + 1, pt0.Y - 1)), 0.0001); // E
      Assert.AreEqual(180.0, CornerkickGame.Tool.getPhi(pt0, new Point(pt0.X + 1, pt0.Y)), 0.0001); // D
      Assert.AreEqual(240.0, CornerkickGame.Tool.getPhi(pt0, new Point(pt0.X + 1, pt0.Y + 1)), 0.0001); // X
      Assert.AreEqual(300.0, CornerkickGame.Tool.getPhi(pt0, new Point(pt0.X - 1, pt0.Y + 1)), 0.0001); // Y
    }

    [TestMethod]
    public void TestPhiRel()
    {
      CornerkickGame.Player pl1 = new CornerkickGame.Player();
      CornerkickGame.Player pl2 = new CornerkickGame.Player();

      pl1.ptPos = new Point(61, 0);
      pl2.ptPos = new Point(1, 0);

      pl1.iLookAt = 0;
      Assert.AreEqual(0.0, CornerkickGame.Tool.getPhiRel(pl1, pl2), 0.0001);

      pl1.iLookAt = 1;
      Assert.AreEqual(-60.0, CornerkickGame.Tool.getPhiRel(pl1, pl2), 0.0001);

      pl1.iLookAt = 2;
      Assert.AreEqual(-120.0, CornerkickGame.Tool.getPhiRel(pl1, pl2), 0.0001);

      pl1.iLookAt = 3;
      Assert.AreEqual(-180.0, CornerkickGame.Tool.getPhiRel(pl1, pl2), 0.0001);

      pl1.iLookAt = 4;
      Assert.AreEqual(+120.0, CornerkickGame.Tool.getPhiRel(pl1, pl2), 0.0001);

      pl1.iLookAt = 5;
      Assert.AreEqual(+60.0, CornerkickGame.Tool.getPhiRel(pl1, pl2), 0.0001);

      pl1.ptPos = new Point(61, 0);
      pl2.ptPos = new Point(122, 0);

      pl1.iLookAt = 0;
      Assert.AreEqual(+180.0, CornerkickGame.Tool.getPhiRel(pl1, pl2), 0.0001);

      pl1.iLookAt = 1;
      Assert.AreEqual(+120.0, CornerkickGame.Tool.getPhiRel(pl1, pl2), 0.0001);

      pl1.iLookAt = 2;
      Assert.AreEqual(+60.0, CornerkickGame.Tool.getPhiRel(pl1, pl2), 0.0001);

      pl1.iLookAt = 3;
      Assert.AreEqual(0.0, CornerkickGame.Tool.getPhiRel(pl1, pl2), 0.0001);

      pl1.iLookAt = 4;
      Assert.AreEqual(-60.0, CornerkickGame.Tool.getPhiRel(pl1, pl2), 0.0001);

      pl1.iLookAt = 5;
      Assert.AreEqual(-120.0, CornerkickGame.Tool.getPhiRel(pl1, pl2), 0.0001);
    }

    [TestMethod]
    public void TestAngleKeeperShooter()
    {
      Random rnd = new Random();

      CornerkickGame.Player plKeeper = new CornerkickGame.Player(6);
      CornerkickGame.Player plShooter = new CornerkickGame.Player(6);

      // Test specific positions
      plKeeper.ptPos = new Point(1, 0);
      plShooter.ptPos = new Point(25, 0);
      Assert.AreEqual(1.0, CornerkickGame.Game.calcAngleKeeperShooter(plKeeper, plShooter, game.ptPitch.X, game.fConvertDist2Meter), 0.0001);

      plKeeper.ptPos = new Point(12, +5);
      plShooter.ptPos = new Point(25, +10);
      Assert.AreEqual(1.0, CornerkickGame.Game.calcAngleKeeperShooter(plKeeper, plShooter, game.ptPitch.X, game.fConvertDist2Meter), 0.0001);

      plKeeper.ptPos = new Point(12, -5);
      plShooter.ptPos = new Point(25, +10);
      Assert.AreEqual(0.078268, CornerkickGame.Game.calcAngleKeeperShooter(plKeeper, plShooter, game.ptPitch.X, game.fConvertDist2Meter), 0.0001);

      // Test random positions
      for (int i = 0; i < 1000; i++) {
        plKeeper.ptPos.X = rnd.Next(game.ptPitch.X);
        plKeeper.ptPos.Y = rnd.Next(-game.ptPitch.Y, game.ptPitch.Y);
        plShooter.ptPos.X = rnd.Next(game.ptPitch.X);
        plShooter.ptPos.Y = rnd.Next(-game.ptPitch.Y, game.ptPitch.Y);
        if (CornerkickGame.Game.calcAngleKeeperShooter(plKeeper, plShooter, game.ptPitch.X, game.fConvertDist2Meter) > 1.0) {
          Debug.WriteLine(CornerkickGame.Game.calcAngleKeeperShooter(plKeeper, plShooter, game.ptPitch.X, game.fConvertDist2Meter));
        }
        Assert.AreEqual(true, CornerkickGame.Game.calcAngleKeeperShooter(plKeeper, plShooter, game.ptPitch.X, game.fConvertDist2Meter) <= 1.0);
      }
    }

    [TestMethod]
    public void TestShootout()
    {
      CornerkickGame.Game gameTest = game.tl.getDefaultGame();
      gameTest.next();
      gameTest.iStandard = 0;
      gameTest.tsMinute = new TimeSpan(2, 0, 0); // Set game time to 120 min.
      gameTest.data.bFirstHalf = false;
      gameTest.data.bOtPossible = true;
      gameTest.data.bOvertime = true;

      // Loop until shootout
      while (!gameTest.data.bShootout) gameTest.next();

      int iShootoutCounter = 0;
      int iHA = -1;
      while (gameTest.next() > 0) {
        if (gameTest.data.bShootout) {
          if (iShootoutCounter % 3 == 0) {
            if (iHA < 0) iHA = gameTest.ball.plAtBall.iHA;

            Assert.AreEqual(gameTest.iPenaltyX, gameTest.ball.ptPos.X);
            Assert.AreEqual(0, gameTest.ball.ptPos.Y);
            Assert.AreEqual(gameTest.ball.ptPos, gameTest.ball.plAtBall.ptPos);

            Assert.AreEqual(iHA, gameTest.ball.plAtBall.iHA);

            iHA = 1 - iHA;
          }

          iShootoutCounter++;
        }
      }

      Assert.AreEqual(true, gameTest.data.team[0].iGoals != gameTest.data.team[1].iGoals);
      Assert.AreEqual(true, Math.Max(gameTest.data.team[0].iGoals, gameTest.data.team[1].iGoals) > 2);
    }

    [TestMethod]
    public void TestMoralResult()
    {
      CornerkickGame.Game gameTest6 = game.tl.getDefaultGame(iPlayerSkills: 6);
      CornerkickGame.Game gameTest8 = game.tl.getDefaultGame(iPlayerSkills: 8);

      // Weak team wins against weak team
      gameTest6 = game.tl.getDefaultGame(iPlayerSkills: 6);
      CornerkickGame.Game.setMoralResult(2, 0, new CornerkickGame.Player[][] { gameTest6.player[0], gameTest6.player[1] }, 11, 0);
      Assert.AreEqual(1.03, getMoralAve(gameTest6.player[0], 11), 0.0001); // Moral increase: +3%
      Assert.AreEqual(0.97, getMoralAve(gameTest6.player[1], 11), 0.0001); // Moral decrease: -3%

      // Strong team wins against weak team
      gameTest6 = game.tl.getDefaultGame(iPlayerSkills: 6);
      gameTest8 = game.tl.getDefaultGame(iPlayerSkills: 8);
      CornerkickGame.Game.setMoralResult(2, 0, new CornerkickGame.Player[][] { gameTest8.player[0], gameTest6.player[1] }, 11, 0);
      Assert.AreEqual(1.012500, getMoralAve(gameTest8.player[0], 11), 0.0001); // Moral increase: +1.25%
      Assert.AreEqual(0.983125, getMoralAve(gameTest6.player[1], 11), 0.0001); // Moral decrease: -1.6875%

      // Weak team wins against strong team
      gameTest6 = game.tl.getDefaultGame(iPlayerSkills: 6);
      gameTest8 = game.tl.getDefaultGame(iPlayerSkills: 8);
      CornerkickGame.Game.setMoralResult(0, 2, new CornerkickGame.Player[][] { gameTest8.player[0], gameTest6.player[1] }, 11, 0);
      Assert.AreEqual(0.946666, getMoralAve(gameTest8.player[0], 11), 0.0001); // Moral decrease: -5.3%
      Assert.AreEqual(1.061111, getMoralAve(gameTest6.player[1], 11), 0.0001); // Moral increase: +6.1%

      // Weak team playes draw against strong team
      gameTest6 = game.tl.getDefaultGame(iPlayerSkills: 6);
      gameTest8 = game.tl.getDefaultGame(iPlayerSkills: 8);
      CornerkickGame.Game.setMoralResult(2, 2, new CornerkickGame.Player[][] { gameTest8.player[0], gameTest6.player[1] }, 11, 0);
      Assert.AreEqual(0.995625, getMoralAve(gameTest8.player[0], 11), 0.0001); // Moral decrease: -0.4%
      Assert.AreEqual(1.007778, getMoralAve(gameTest6.player[1], 11), 0.0001); // Moral increase: +0.8%

      // Weak with low moral team wins against weak team with high moral
      gameTest6 = game.tl.getDefaultGame(iPlayerSkills: 6);
      for (int iP = 0; iP < gameTest6.player[0].Length; iP++) gameTest6.player[0][iP].fMoral *= 0.9f;
      for (int iP = 0; iP < gameTest6.player[1].Length; iP++) gameTest6.player[1][iP].fMoral *= 1.1f;
      CornerkickGame.Game.setMoralResult(2, 0, new CornerkickGame.Player[][] { gameTest6.player[0], gameTest6.player[1] }, 11, 0);
      Assert.AreEqual(0.9595385193824768, getMoralAve(gameTest6.player[0], 11), 0.0001); // Moral increase: +6.14%
      Assert.AreEqual(1.0471595525741577, getMoralAve(gameTest6.player[1], 11), 0.0001); // Moral decrease: -5.42%

      // Weak with high moral team wins against weak team with low moral
      gameTest6 = game.tl.getDefaultGame(iPlayerSkills: 6);
      for (int iP = 0; iP < gameTest6.player[0].Length; iP++) gameTest6.player[0][iP].fMoral *= 1.1f;
      for (int iP = 0; iP < gameTest6.player[1].Length; iP++) gameTest6.player[1][iP].fMoral *= 0.9f;
      CornerkickGame.Game.setMoralResult(2, 0, new CornerkickGame.Player[][] { gameTest6.player[0], gameTest6.player[1] }, 11, 0);
      Assert.AreEqual(1.1144455671310425, getMoralAve(gameTest6.player[0], 11), 0.0001); // Moral increase: +1.39%
      Assert.AreEqual(0.8833065032958984, getMoralAve(gameTest6.player[1], 11), 0.0001); // Moral decrease: -1.63%
    }
    private float getMoralAve(CornerkickGame.Player[] pl, byte nPl)
    {
      float fMoralAve = 0f;

      for (byte iP = 0; iP < nPl; iP++) fMoralAve += pl[iP].fMoral;
      fMoralAve /= nPl;

      return fMoralAve;
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
    public void TestWinDrawDefeat()
    {
      Assert.AreEqual(+1, CornerkickGame.Tool.getWinDrawDefeat(1, 0));
      Assert.AreEqual(0, CornerkickGame.Tool.getWinDrawDefeat(1, 1));
      Assert.AreEqual(-1, CornerkickGame.Tool.getWinDrawDefeat(0, 1));

      Assert.AreEqual(0, CornerkickGame.Tool.getWinDrawDefeat(1, 0, 1, 0));
      Assert.AreEqual(-1, CornerkickGame.Tool.getWinDrawDefeat(1, 1, 1, 0));
      Assert.AreEqual(+1, CornerkickGame.Tool.getWinDrawDefeat(1, 1, 2, 2));
      Assert.AreEqual(+1, CornerkickGame.Tool.getWinDrawDefeat(1, 0, 2, 1));
      Assert.AreEqual(-1, CornerkickGame.Tool.getWinDrawDefeat(3, 2, 1, 0));
    }

    [TestMethod]
    public void TestKeeperSubAfterRedCard()
    {
      CornerkickGame.Game gameTest = game.tl.getDefaultGame();
      gameTest.data.team[0].bTeamUser = false;

      gameTest.next();

      gameTest.giveCardRed(gameTest.tl.getKeeper(true));
      while (gameTest.next() > 0) {
      }
    }

    [TestMethod]
    public void TestTraining()
    {
      const byte iTrainingsPerDayMax = 3;
      const float fCondiIni = 0.8f;
      const double fDeltaErr = 0.005;
      byte[] iTrainingType = { 2, 1 };

      float fCondi0 = 0f;
      float fFresh0 = 0f;
      float fMood0 = 0f;
      for (byte iTrPerDay = 1; iTrPerDay <= iTrainingsPerDayMax; iTrPerDay++) {
        for (byte iType = 0; iType < iTrainingType.Length; iType++) {
          for (byte i = 0; i < 3; i++) {
            CornerkickManager.Main mn = new CornerkickManager.Main(iTrainingsPerDay: iTrPerDay);
            mn.dtDatum = mn.dtDatum.AddDays(1);

            CornerkickManager.User usr = new CornerkickManager.User();
            mn.ltUser.Add(usr);

            // Create Club
            CornerkickManager.Club clb = new CornerkickManager.Club();
            clb.iId = 0;
            clb.sName = "Team";
            clb.iLand = 0;
            clb.iDivision = 0;
            clb.user = usr;
            CornerkickManager.Main.TrainingPlan.Unit tu = new CornerkickManager.Main.TrainingPlan.Unit();
            tu.dt = mn.dtDatum;
            tu.iType = (sbyte)iTrainingType[iType];
            clb.training.ltUnit.Add(tu); // Condition
            clb.staff.iCondiTrainer = 4;
            clb.buildings.bgGym.iLevel = 2;
            mn.ltClubs.Add(clb);

            usr.club = clb;

            CornerkickManager.Player pl = new CornerkickManager.Player();
            pl.fCondition = fCondiIni;
            pl.fFresh = 0.8f;
            pl.fMoral = 0.8f;
            pl.contract = CornerkickManager.PlayerTool.getContract(pl, 1, clb, mn.dtDatum, mn.dtSeasonEnd);
            pl.dtBirthday = mn.dtDatum.AddYears(-25);
            clb.ltPlayer.Add(pl);

            if (i == 1) { // Test trainer level 5
              clb.staff.iCondiTrainer = 5;
            } else if (i == 2) { // Test training camp
              CornerkickManager.TrainingCamp.Camp cmp = mn.tcp.ltCamps[1];
              CornerkickManager.TrainingCamp.bookCamp(ref clb, cmp, mn.dtDatum.AddDays(-1), mn.dtDatum.AddDays(+1), mn.dtDatum, mn.settings.tsTrainingLength);
            } else if (i == 3) { // Test doping
              pl.doDoping(mn.ltDoping[1]);
            }

            CornerkickManager.TrainingCamp.Booking tcb = CornerkickManager.TrainingCamp.getCurrentCamp(clb, mn.dtDatum);

            float fCondiPre = pl.fCondition;
            float fFreshPre = pl.fFresh;
            float fMoodPre = pl.fMoral;

            for (byte iT = 0; iT < iTrPerDay; iT++) {
              mn.plt.doTraining(ref pl, tu.iType, mn.dtDatum, campBooking: tcb);
            }

            // Test
            if (i == 0) { // Test common training increase of condi. (4.89%)
              if (iTrainingType[iType] == 2) Assert.AreEqual(0.8038313984870911, pl.fCondition, fDeltaErr); // Condi
              else if (iTrainingType[iType] == 1) Assert.AreEqual(0.785000, pl.fCondition, fDeltaErr); // Fresh

              fCondi0 = pl.fCondition;
              fFresh0 = pl.fFresh;
              fMood0 = pl.fMoral;
            } else if (i == 1) { // Test training increase with trainer level 5
              if (iTrainingType[iType] == 2) {
                Assert.AreEqual(1.0077719688415527, pl.fCondition / fCondiPre, fDeltaErr);
                Assert.AreEqual(0.9370000, pl.fFresh / fFreshPre, fDeltaErr);
                Assert.AreEqual(0.9843750, pl.fMoral / fMoodPre, fDeltaErr);

                Assert.AreEqual(1.0025042, pl.fCondition / fCondi0, fDeltaErr);
                Assert.AreEqual(1.0000000, pl.fFresh / fFresh0, fDeltaErr);
                Assert.AreEqual(1.0000000, pl.fMoral / fMood0, fDeltaErr);
              }
            } else if (i == 2) { // Test training increase with training camp
              if (iTrainingType[iType] == 2) {
                Assert.AreEqual(1.0496429204940796, pl.fCondition / fCondiPre, fDeltaErr);
                Assert.AreEqual(0.9515384, pl.fFresh / fFreshPre, fDeltaErr);
                Assert.AreEqual(0.9886364, pl.fMoral / fMoodPre, fDeltaErr);

                Assert.AreEqual(1.0446399450302124, pl.fCondition / fCondi0, fDeltaErr);
                Assert.AreEqual(1.0155160, pl.fFresh / fFresh0, fDeltaErr);
                Assert.AreEqual(1.0021474, pl.fMoral / fMood0, fDeltaErr);
              }
            } else if (i == 3) { // Test training increase with doping
              if (iTrainingType[iType] == 2) {
                Assert.AreEqual(1.0378909, pl.fCondition / fCondiPre, fDeltaErr);
                Assert.AreEqual(1.0000000, pl.fFresh / fFreshPre, fDeltaErr);
                Assert.AreEqual(1.0018748, pl.fMoral / fMoodPre, fDeltaErr);

                Assert.AreEqual(1.0378909, pl.fCondition / fCondi0, fDeltaErr);
                Assert.AreEqual(1.0000000, pl.fFresh / fFresh0, fDeltaErr);
                Assert.AreEqual(1.0018748, pl.fMoral / fMood0, fDeltaErr);
              }
            }
          }
        }
      }
    }

    [TestMethod]
    public void TestShortestDistance()
    {
      /*
      */
      CornerkickGame.Game gameTest = game.tl.getDefaultGame();

      // Test 1
      Point pt10 = new Point(-2, +1);
      Point pt11 = new Point(-6, 0);
      Point pt1A = new Point(+10, +5);
      Point pt1F = new Point();

      float fDist1 = CornerkickGame.Tool.getShortestDistance(pt10, pt11, pt1A, out pt1F, gameTest.fConvertDist2Meter);

      Assert.AreEqual(14.727, fDist1, 0.00001);
      Assert.AreEqual(-5, pt1F.X);
      Assert.AreEqual(0, pt1F.Y);

      // Test 2
      Point pt20 = new Point(-40, +1);
      Point pt21 = new Point(+6, -11);
      Point pt2A = new Point(-20, -5);
      Point pt2F = new Point();

      float fDist2 = CornerkickGame.Tool.getShortestDistance(pt20, pt21, pt2A, out pt2F, gameTest.fConvertDist2Meter);

      Assert.AreEqual(0.7572657, fDist2, 0.00001);
      Assert.AreEqual(-19, pt2F.X);
      Assert.AreEqual(-4, pt2F.Y);
      /*
      Assert.AreEqual(0.9701425, fDist, 0.00001);
      Assert.AreEqual(10, ptF.X);
      Assert.AreEqual( 4, ptF.Y);
       */
    }

    [TestMethod]
    public void TestPlayerValue()
    {
      CornerkickManager.Main mn = new CornerkickManager.Main();

      CornerkickManager.Player pl = new CornerkickManager.Player(7);

      float fAge = 25f;

      // Player value should be 2.493 mio €
      pl.plGame.fExperiencePos[10] = 1.0f;
      Assert.AreEqual(2493, pl.getValue(fAge, mn.dtSeasonEnd, 1000), 0.00001);

      // Player value should be 2.964 mio €
      pl.plGame.fExperiencePos[8] = 0.5f;
      pl.plGame.fExperiencePos[9] = 0.5f;
      Assert.AreEqual(2964, pl.getValue(fAge, mn.dtSeasonEnd, 1000), 0.00001);

      // Player value should be 17.583 mio €
      fAge = 16f;
      for (int iT = 0; iT < pl.iTalent.Length; iT++) pl.iTalent[iT] = 9;
      Assert.AreEqual(17583, pl.getValue(fAge, mn.dtSeasonEnd, 1000), 0.00001);
    }

    [TestMethod]
    public void TestSetPlayerIndTraining()
    {
      const int nTests = 10000;
      int iCounterReaction = 0;
      int iCounterShootAcc = 0;
      for (int i = 0; i < nTests; i++) {
        // Keeper (Kp)
        CornerkickGame.Player plKp = new CornerkickGame.Player(7);
        for (byte iP = 0; iP < plKp.fExperiencePos.Length; iP++) plKp.fExperiencePos[iP] = 0.3f;
        plKp.fExperiencePos[0] = 1.0f; // Keeper

        byte iIndTrKp = CornerkickManager.PlayerTool.getTrainingInd(plKp);
        if (iIndTrKp == 13) iCounterReaction++;

        // Should never be ...
        Assert.AreEqual(false, iIndTrKp == 2); // ... duel offence
        Assert.AreEqual(false, iIndTrKp == 10); // ... header
        Assert.AreEqual(false, iIndTrKp == 11); // ... free-kick

        // Center foreward (CF)
        CornerkickGame.Player plCF = new CornerkickGame.Player(7);
        for (byte iP = 0; iP < plCF.fExperiencePos.Length; iP++) plCF.fExperiencePos[iP] = 0.3f;
        plCF.fExperiencePos[10] = 1.0f; // CF

        byte iIndTrCF = CornerkickManager.PlayerTool.getTrainingInd(plCF);
        if (iIndTrCF == 9) iCounterShootAcc++;

        // Should never be ...
        Assert.AreEqual(false, iIndTrCF == 13); // ... reaction
        Assert.AreEqual(false, iIndTrCF == 15); // ... catch
      }

      Assert.AreEqual(4.0 / 21.0, iCounterReaction / (float)nTests, 0.01); // Reaction
      Assert.AreEqual(5.0 / 38.0, iCounterShootAcc / (float)nTests, 0.01); // Shoot acc.
    }

    [TestMethod]
    public void TestNegotiateContract()
    {
      const int iRepeat = 10000;
      const int iContractLength = 2;
      const int iGamesPerSeason = 30;

      CornerkickManager.Main mn = new CornerkickManager.Main();

      CornerkickManager.Club clb = new CornerkickManager.Club();
      CornerkickManager.Player pl = new CornerkickManager.Player(7);

      pl.plGame.dtBirthday = new DateTime(mn.dtDatum.Year - 25, 1, 1);
      pl.plGame.fExperiencePos[10] = 1.0f;

      CornerkickManager.Player.Contract ctrReq = CornerkickManager.PlayerTool.getContract(pl, iContractLength, clb, mn.dtDatum, mn.dtSeasonEnd, iGamesPerSeason: iGamesPerSeason);

      ///////////////////////////////////////////////////////////////////
      // Test bonus offered = bonus req.
      double fMood = 0.0;
      for (int iN = 0; iN < iRepeat; iN++) {
        mn.ltTransfer.Clear();
        CornerkickManager.Player.Contract ctrNew = mn.plt.negotiatePlayerContract(pl, clb, 2, iSalaryOffer: (int)(ctrReq.iSalary * 0.9), iBonusPlayOffer: ctrReq.iPlay, iBonusGoalOffer: ctrReq.iGoal, iGamesPerSeason: iGamesPerSeason);
        fMood += ctrNew.fMood;
      }
      fMood /= iRepeat;

      // Test average mood
      Assert.AreEqual(0.607, fMood, 0.01);

      ///////////////////////////////////////////////////////////////////
      // Test bonus offered < bonus req.
      fMood = 0.0;
      for (int iN = 0; iN < iRepeat; iN++) {
        mn.ltTransfer.Clear();
        CornerkickManager.Player.Contract ctrNew = mn.plt.negotiatePlayerContract(pl, clb, 2, iSalaryOffer: (int)(ctrReq.iSalary * 0.9), iBonusPlayOffer: (int)(ctrReq.iPlay * 0.9), iBonusGoalOffer: (int)(ctrReq.iGoal * 0.9), iGamesPerSeason: iGamesPerSeason);
        fMood += ctrNew.fMood;
      }
      fMood /= iRepeat;

      // Test average mood
      Assert.AreEqual(0.570, fMood, 0.01);
    }

    [TestMethod]
    public void TestScouting()
    {
      CornerkickManager.Main.Staff.Scout scout = new CornerkickManager.Main.Staff.Scout(iSkill: 5);
      //Assert.AreEqual(Math.Sqrt(0.5), scout.getSigma());

      //scout = new CornerkickManager.Main.Staff.Scout(iSkill: 4);
      Assert.AreEqual(1.0, scout.getSigma());

      Assert.AreEqual(0.158655253931457, CornerkickGame.Tool.normal_dist(-1, scout.getSigma()), 0.000001); // 15.866% * 2 = 31.73% risk for -1/+1
      Assert.AreEqual(0.022750131948179, CornerkickGame.Tool.normal_dist(-2, scout.getSigma()), 0.000001); //  2.275% * 2 =  4.55% risk for -2/+2
      Assert.AreEqual(0.001349898031630, CornerkickGame.Tool.normal_dist(-3, scout.getSigma()), 0.000001); //  0.135% * 2 =  0.27% risk for -3/+3
      Assert.AreEqual(0.841344746068543, CornerkickGame.Tool.normal_dist(+1, scout.getSigma()), 0.000001);

      /* Skill = 4
      Assert.AreEqual(0.124106539494962, CornerkickGame.Tool.normal_dist(-1, scout.getSigma()), 0.000001); // 12.411% * 2 = 24.82% risk for -1/+1
      Assert.AreEqual(0.010460667668897, CornerkickGame.Tool.normal_dist(-2, scout.getSigma()), 0.000001); //  1.046% * 2 =  2.09% risk for -2/+2
      Assert.AreEqual(0.000266002752570, CornerkickGame.Tool.normal_dist(-3, scout.getSigma()), 0.000001); //  0.027% * 2 =  0.05% risk for -3/+3
      Assert.AreEqual(0.875893460505038, CornerkickGame.Tool.normal_dist(+1, scout.getSigma()), 0.000001);
      */

      CornerkickGame.Player pl = new CornerkickGame.Player(7);

      double[] fScRnd = { 0.15, 0.16, 0.84, 0.85 };
      int[] iScRes = { -1, 0, 0, +1 };
      for (int i = 0; i < fScRnd.Length; i++) {
        List<CornerkickManager.Main.Staff.Scout.PlayerData.Details> ltScDetails = scout.scoutPlayer(pl, DateTime.Now, rndScouting: fScRnd[i]);
        Assert.AreEqual(1, ltScDetails.Count);
        Assert.AreEqual(iScRes[i], ltScDetails[0].iSkill - pl.iSkill[ltScDetails[0].iSkillIx]);
      }
    }

    [TestMethod]
    public void TestToilets()
    {
      const int iSpecPotVIP = 960;
      int iToilets = 0;
      int iSpec = 0;

      int iToiletsReq = iSpecPotVIP / (CornerkickManager.Stadium.iFeatureSpecReqToilets * CornerkickManager.Stadium.iSpecReqFac[2]);

      // 0%
      iToilets = 0;
      iSpec = (int)(iSpecPotVIP * CornerkickManager.Finance.getCustomerReductionFactor(iToilets, CornerkickManager.Stadium.iFeatureSpecReqToilets * CornerkickManager.Stadium.iSpecReqFac[2], iSpecPotVIP));
      Assert.AreEqual(720, iSpec);

      // 25%
      iToilets = (1 * iToiletsReq) / 4;
      iSpec = (int)(iSpecPotVIP * CornerkickManager.Finance.getCustomerReductionFactor(iToilets, CornerkickManager.Stadium.iFeatureSpecReqToilets * CornerkickManager.Stadium.iSpecReqFac[2], iSpecPotVIP));
      Assert.AreEqual(780, iSpec);

      // 50%
      iToilets = (2 * iToiletsReq) / 4;
      iSpec = (int)(iSpecPotVIP * CornerkickManager.Finance.getCustomerReductionFactor(iToilets, CornerkickManager.Stadium.iFeatureSpecReqToilets * CornerkickManager.Stadium.iSpecReqFac[2], iSpecPotVIP));
      Assert.AreEqual(840, iSpec);

      // 75%
      iToilets = (3 * iToiletsReq) / 4;
      iSpec = (int)(iSpecPotVIP * CornerkickManager.Finance.getCustomerReductionFactor(iToilets, CornerkickManager.Stadium.iFeatureSpecReqToilets * CornerkickManager.Stadium.iSpecReqFac[2], iSpecPotVIP));
      Assert.AreEqual(900, iSpec);

      // 99%
      iToilets = (4 * iToiletsReq) / 4;
      Assert.AreEqual(960.75, (iSpecPotVIP + 1) * CornerkickManager.Finance.getCustomerReductionFactor(iToilets, CornerkickManager.Stadium.iFeatureSpecReqToilets * CornerkickManager.Stadium.iSpecReqFac[2], iSpecPotVIP + 1), 0.001);

      // 100%
      iToilets = (4 * iToiletsReq) / 4;
      iSpec = (int)(iSpecPotVIP * CornerkickManager.Finance.getCustomerReductionFactor(iToilets, CornerkickManager.Stadium.iFeatureSpecReqToilets * CornerkickManager.Stadium.iSpecReqFac[2], iSpecPotVIP));
      Assert.AreEqual(960, iSpec);
    }

  }

  [TestClass]
  public class RegressionTests
  {
    CornerkickGame.Game game = new CornerkickGame.Game(new CornerkickGame.Game.Data());

    [TestMethod]
    public void TestGamesDirect()
    {
      const int nGames = 100;
      const float fRefereeCorruptHome = 0f;
      const bool bInjuriesPossible = true;
      const byte iPlayerSkillsH = 8;
      const byte iPlayerSkillsA = 8;
      const byte iPlayerSkillSpeedExactH = 0;
      const byte iPlayerSkillSpeedExactA = 0;
      const byte iPlayerSkillSpeedMax = 8;
      sbyte iFormationIx = -1;

      const bool bTestPlayerSamePosition = false; // Tests if players are on same position
      const bool bTestBallPosition = false; // Tests if ball stays at position for iBallCounterMax times
      const bool bTestPlayerStep = false;
      const bool bTestCornerkick = false;
      const bool bTestGrade = false;
      const bool bTestHA = false;
      const bool bManMarking = false;

      if (bManMarking) iFormationIx = 7;

      Random rnd = new Random();
      CornerkickManager.Main mn = new CornerkickManager.Main();
      string sTestResultLog = Path.Combine(mn.settings.sHomeDir, "Test_Results.txt");
      DateTime dtStart = DateTime.Now;
      Utility.PostGamesData pgd = new Utility.PostGamesData();

      Utility.PostGamesHeader(sTestResultLog, dtStart, nGames, iPlayerSkillsH, iPlayerSkillsA);

      Stopwatch sw = new Stopwatch();
      sw.Start();

#if _DoE
      float fWf1 = 0.5f;
      const float fWfStep = 0.2f;
      const float fWfMax  = 2.0f;

      StreamWriter swLog = new StreamWriter(CornerkickManager.Main.sHomeDir + "/Test_Results_DoE.txt", false);
      swLog.WriteLine("Wf1 Wf2 chance_goal_H chance_goal_A H/A");
      swLog.Close();

      while (fWf1 < fWfMax) { // First loop
        float fWf2 = 0.5f;

        while (fWf2 < fWfMax) { // Second loop
#endif
      int[] iShootRange = new int[8];
      int[] iShootRangeGoals = new int[8];
      double[] fGrade = new double[11]; // Average grade depending on position

#if _ML
          double fWfPass = 1f;
          double fWfPassCounter = 0;
#endif

      for (int iG = 0; iG < nGames; iG++) {
        // Create default game
        CornerkickGame.Game gameTest = game.tl.getDefaultGame(iPlayerSkillsH: iPlayerSkillsH, iPlayerSkillsA: iPlayerSkillsA);
        gameTest.data.bInjuriesPossible = bInjuriesPossible;
        gameTest.data.bCardsPossible = true;

        for (int iHA = 0; iHA < 2; iHA++) {
          for (int iPl = 0; iPl < gameTest.player[iHA].Length; iPl++) {
            if (iHA == 0 && iPlayerSkillSpeedExactH > 0) gameTest.player[iHA][iPl].iSkill[0] = iPlayerSkillSpeedExactH;
            else if (iHA == 1 && iPlayerSkillSpeedExactA > 0) gameTest.player[iHA][iPl].iSkill[0] = iPlayerSkillSpeedExactA;

            gameTest.player[iHA][iPl].iSkill[0] = Math.Min(gameTest.player[iHA][iPl].iSkill[0], iPlayerSkillSpeedMax);
          }
        }

        // Shuffle formations
        for (byte iHA = 0; iHA < 2; iHA++) {
          int iFrm = 0;
          if (iFormationIx >= 0 && iFormationIx < mn.ltFormationen.Count) iFrm = iFormationIx;
          else iFrm = rnd.Next(mn.ltFormationen.Count);

          gameTest.data.team[iHA].ltTactic[0].formation = mn.ltFormationen[iFrm];
          CornerkickManager.Main.doFormation(gameTest.player[iHA], gameTest.data.team[iHA].ltTactic[0].formation, gameTest.data.nPlStart, gameTest.data.nPlRes, gameTest.ptPitch, 0, new DateTime());

          // Test if keeper in goal
          if (gameTest.player[iHA][0].fExperiencePos[0] < 1f) {
            CornerkickManager.Main.doFormation(gameTest.player[iHA], gameTest.data.team[iHA].ltTactic[0].formation, gameTest.data.nPlStart, gameTest.data.nPlRes, gameTest.ptPitch, 0, new DateTime());
            CornerkickManager.Main.doFormation(gameTest.player[iHA], gameTest.data.team[iHA].ltTactic[0].formation, gameTest.data.nPlStart, gameTest.data.nPlRes, gameTest.ptPitch, 0, new DateTime());
          }
          Assert.AreEqual(1.0, gameTest.player[iHA][0].fExperiencePos[0], 0.001);

          float fSkillAve = 0f;
          for (byte iPl = 0; iPl < gameTest.data.nPlStart; iPl++) {
            fSkillAve += CornerkickGame.Tool.getAveSkill(gameTest.player[iHA][iPl]);
          }
          fSkillAve /= gameTest.data.nPlStart;
          //Assert.AreEqual(7.0173, fSkillAve, 0.001);
        }

        // Man-marking
        if (bManMarking) {
          gameTest.player[0][1].iIxManMarking = 10;
          gameTest.player[0][2].iIxManMarking = 9;
        }

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

        // Test correct set of jersey numbers
        gameTest.player[0][0].iNr = 0;
        gameTest.player[0][1].iNr = 2;
        gameTest.player[0][2].iNr = 2;
        gameTest.player[0][3].iNr = 3;

        // Do an initial step
        gameTest.next();

        // Check jersey numbers
        for (byte iHA = 0; iHA < 2; iHA++) {
          for (byte iPl = 0; iPl < gameTest.player[iHA].Length; iPl++) {
            byte iJerseyNb = gameTest.player[iHA][iPl].iNr;
            Assert.AreEqual(false, iJerseyNb == 0);

            for (int jPl = iPl + 1; jPl < gameTest.player[iHA].Length; jPl++) {
              Assert.AreEqual(false, iJerseyNb == gameTest.player[iHA][jPl].iNr);
            }
          }
        }

        const int iBallCounterMax = 200;
        int iBallCounter = 0;
        Point ptBall = gameTest.ball.ptPos;

        int iGoalH = 0;
        int iGoalA = 0;
        int iShootRes = -1;
        int iGameTestRet = 0;
        while ((iGameTestRet = gameTest.next()) > 0) {
          // Test remaining steps
          if (bTestPlayerStep) {
            for (byte iHA = 0; iHA < 2; iHA++) {
              for (byte iPl = 0; iPl < gameTest.data.nPlStart; iPl++) {
                CornerkickGame.Player plStep = gameTest.player[iHA][iPl];
                Assert.AreEqual(true, plStep.fStepsAll <= plStep.fSteps + 0.00001f,
                  /*gameTest.data.ltState[gameTest.data.ltState.Count - 1].ltComment[gameTest.data.ltState[gameTest.data.ltState.Count - 1].ltComment.Count - 1].sText + */
                  ", ball pos: " + gameTest.ball.ptPos.X.ToString() + "/" + gameTest.ball.ptPos.Y.ToString() +
                  ", (game: " + iG.ToString() + ")");
              }
            }
          }

          // Test cornerkick
          if (iShootRes >= 0 && bTestCornerkick) {
            if (iShootRes == 4) { // Cornerkick
              Assert.AreEqual(3, Math.Abs(gameTest.iStandard), "Standard is not cornerkick but " + Math.Abs(gameTest.iStandard).ToString());
              Assert.AreEqual(true, gameTest.ball.ptPos.X <= 0 || gameTest.ball.ptPos.X >= gameTest.ptPitch.X);
            }
          }
          iShootRes = -1;

          if (CornerkickGame.Tool.correctPos(ref gameTest.ball.ptPos, gameTest.ptPitch.X)) gameTest.tl.writeState();

          Assert.AreEqual(false, CornerkickGame.Tool.correctPos(ref gameTest.ball.ptPos, gameTest.ptPitch.X), "Ball Position [" + ptBall.X + "/" + ptBall.Y + "] corrected. (Minute: " + gameTest.tsMinute.ToString() + ")");

          // Test player indexes
          for (byte iHA = 0; iHA < 2; iHA++) {
            for (byte iPl = 0; iPl < gameTest.data.nPlStart; iPl++) {
              Assert.AreEqual(iPl, gameTest.player[iHA][iPl].iIndex);
            }
          }

          if (bTestBallPosition) {
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
          }

          // Test player positions
          if (bTestPlayerSamePosition) Assert.AreEqual(false, CornerkickGame.Game.checkPlayerOnSamePosition(gameTest.player, gameTest.data.nPlStart), "Player are on same positon (Minute: " + gameTest.tsMinute.ToString() + ")");

          // Test player action array
          if (gameTest.ball.plAtBall != null) {
            float[] fAction;
            sbyte iAction = gameTest.ai.getPlayerAction(gameTest.ball.plAtBall, out fAction, false, 0);
            Assert.AreEqual(1.0, Utility.getPlayerActionTotal(fAction), 0.00001);
          }

          CornerkickGame.Game.State stateLast1 = gameTest.data.ltState[gameTest.data.ltState.Count - 2];
          CornerkickGame.Game.State stateLast = gameTest.data.ltState[gameTest.data.ltState.Count - 1];
          CornerkickGame.Game.Shoot shoot = stateLast.shoot;
          /*
        if (shoot.plShoot != null) {
          iShootRes = shoot.iResult;

          float fDistTmp = CornerkickGame.Tool.getDistanceToGoal(shoot.plShoot, gameTest.ptPitch.X, gameTest.fConvertDist2Meter)[0];
          if (fDistTmp > 50) {
            Debug.Write(fDistTmp.ToString("0.0m") + ", ");
            Debug.Write(gameTest.ai.getChanceShootGoal(shoot.plShoot));
            Debug.WriteLine("");
          }
        }
        */

          // Test goal if goals difer from last step
          /*
          if (iGoalH != gameTest.data.team[0].iGoals ||
              iGoalA != gameTest.data.team[1].iGoals) {
          */
          if (shoot != null && shoot.plShoot != null && shoot.iResult == 1 && shoot.bFinished) {
            /*
            if (shoot.plShoot != null && shoot.iResult == 1) {
              float fDistTmp = CornerkickGame.Tool.getDistanceToGoal(shoot.plShoot, gameTest.ptPitch.X, gameTest.fConvertDist2Meter)[0];
              if (fDistTmp > 35) {
                Debug.Write(fDistTmp.ToString("0.0m") + ", ");
                Debug.Write(gameTest.ai.getChanceShootGoal(shoot.plShoot));
                Debug.WriteLine("");
              }
            }
            */
            Utility.testGoal(gameTest, iGoalH != gameTest.data.team[0].iGoals, iGoalH, iGoalA);
          }

          iGoalH = gameTest.data.team[0].iGoals;
          iGoalA = gameTest.data.team[1].iGoals;

          if (bTestHA) {
#if !_DoE
            if ((int)gameTest.tsMinute.TotalMinutes % 10 == 0 && gameTest.tsMinute.Seconds == 0) Utility.testHA(gameTest);
#endif
          }
        } // gameTest.next()

#if _ML
            fWfPass  = gameTest.ai.ml.fWfPass;
            fWfPassCounter += gameTest.ai.ml.fWfPassCounter;

            Debug.WriteLine(" Game " + (iG + 1).ToString()  + ". Result: " + gameTest.data.team[0].iGoals.ToString() + ":" + gameTest.data.team[1].iGoals.ToString() + ", " + gameTest.data.tsMinute.TotalMinutes.ToString("0") + ":" + gameTest.data.tsMinute.Seconds.ToString("00") + ", WfPass: " + fWfPass.ToString("0.0000"));
#else
        //Debug.WriteLine(" Game " + (iG + 1).ToString()  + ". Result: " + gameTest.data.team[0].iGoals.ToString() + ":" + gameTest.data.team[1].iGoals.ToString() + ", " + gameTest.data.tsMinute.TotalMinutes.ToString("0") + ":" + gameTest.data.tsMinute.Seconds.ToString("00"));
#endif

        // Check player experience
        for (byte iHA = 0; iHA < 2; iHA++) {
          for (byte iPl = 0; iPl < gameTest.player[iHA].Length; iPl++) {
            CornerkickGame.Player plExp = gameTest.player[iHA][iPl];

            if (plExp.statGame.iStat[28] > 1) Assert.AreEqual(true, plExp.fExperience > 0f, plExp.sName + " has not gained experience!");
          }
        }

        // Count scorer
        int iScorer = 0;
        foreach (CornerkickGame.Game.State state in gameTest.data.ltState) {
          CornerkickGame.Game.Shoot s = state.shoot;
          if (s.plShoot != null && s.bFinished && s.iResult == 1) {
            iScorer++;
          }
        }
        Assert.AreEqual(iScorer, iGoalH + iGoalA, "Number of scorer not valid!");

        Utility.CollectPostGamesData(gameTest.data, pgd);
      } // for each game

      // Stop stopwatch
      sw.Stop();

      Utility.PostGames(sTestResultLog, pgd, iPlayerSkillsH, iPlayerSkillsA, sw.ElapsedMilliseconds);

#if !_DoE
      if (iPlayerSkillsH == iPlayerSkillsA && iPlayerSkillSpeedMax >= iPlayerSkillsH) {
        if (pgd.iGA > 0) Assert.AreEqual(1.0, pgd.iGH / (double)pgd.iGA, 0.2);
        if (pgd.iShootsA > 0) Assert.AreEqual(1.0, pgd.iShootsH / (double)pgd.iShootsA, 0.2);
        if (pgd.fChanceGoalA > 0) Assert.AreEqual(1.0, pgd.fChanceGoalH / pgd.fChanceGoalA, 0.2);
        if (pgd.fShootDistA > 0) Assert.AreEqual(1.0, pgd.fShootDistH / pgd.fShootDistA, 0.2);
        if (pgd.iDuelA > 0) Assert.AreEqual(1.0, pgd.iDuelH / (double)pgd.iDuelA, 0.2);
        if (pgd.iStepsA > 0) Assert.AreEqual(1.0, pgd.iStepsH / (double)pgd.iStepsA, 0.2);
        if (pgd.iPossA > 0) Assert.AreEqual(1.0, pgd.iPossH / (double)pgd.iPossA, 0.2);
        if (pgd.iOffsiteA > 0) Assert.AreEqual(1.0, pgd.iOffsiteH / (double)pgd.iOffsiteA, 0.2);
      }

      // Test grades
      if (bTestGrade) {
        for (byte iGrd = 0; iGrd < pgd.fGrade.Length; iGrd++) {
          if (pgd.fGrade[iGrd] > 0.0) Assert.AreEqual(3.5, pgd.fGrade[iGrd], 0.2, "Grade: " + iGrd.ToString());
        }
      }
#endif

#if _DoE
          fWf2 += fWfStep;
        }

        fWf1 += fWfStep;
      }
#endif
    }

    [TestMethod]
    public void TestGamesParallel()
    {
      const int nGames = 4000;
      const int nGamesPerChunk = 20; // Do nGamesPerChunk games in parallel 
      const int iGameSpeed = 0; // [ms]
      const byte iPlayerSkillsH = 8;
      const byte iPlayerSkillsA = 8;
      const bool bCardsPossible = true;
      const bool bInjuriesPossible = true;

      string sTestResultLog = "Test_Results_parallel.txt";
      DateTime dtStart = DateTime.Now;
      Utility.PostGamesData pgd = new Utility.PostGamesData();

      Utility.PostGamesHeader(sTestResultLog, dtStart, nGames, iPlayerSkillsH, iPlayerSkillsA);

      Stopwatch sw = new Stopwatch();
      sw.Start();

      int iGames = nGames;
      while (iGames > 0) {
        Debug.WriteLine(" Remaining games: " + iGames.ToString());

        CornerkickManager.Main mn = new CornerkickManager.Main();

        List<CornerkickManager.Main.GameDataPlus> ltGameData = new List<CornerkickManager.Main.GameDataPlus>();

        for (int iG = 0; iG < nGamesPerChunk; iG++) {
          CornerkickGame.Game gameTest = game.tl.getDefaultGame(iPlayerSkillsH: iPlayerSkillsH, iPlayerSkillsA: iPlayerSkillsA, iClubIdStart: mn.ltClubs.Count, iPlIdStart: mn.ltPlayer.Count);
          gameTest.data.bCardsPossible = bCardsPossible;
          gameTest.data.bInjuriesPossible = bInjuriesPossible;

          // Set game speed
          gameTest.data.iGameSpeed = iGameSpeed;

          CornerkickManager.Club clbH = new CornerkickManager.Club();
          CornerkickManager.Club clbA = new CornerkickManager.Club();

          // Set club id's
          clbH.iId = gameTest.data.team[0].iTeamId;
          clbA.iId = gameTest.data.team[1].iTeamId;

          // Set club formations
          clbH.ltTactic[0].formation = mn.ltFormationen[8];
          clbA.ltTactic[0].formation = mn.ltFormationen[8];

          foreach (CornerkickGame.Player pl in gameTest.player[0]) {
            CornerkickManager.Player plMng = new CornerkickManager.Player() { plGame = pl };
            mn.ltPlayer.Add(plMng);
            clbH.ltPlayer.Add(plMng);
          }
          foreach (CornerkickGame.Player pl in gameTest.player[1]) {
            CornerkickManager.Player plMng = new CornerkickManager.Player() { plGame = pl };
            mn.ltPlayer.Add(plMng);
            clbA.ltPlayer.Add(plMng);
          }
          mn.ltClubs.Add(clbH);
          mn.ltClubs.Add(clbA);

          CornerkickManager.Main.GameDataPlus gdp = new CornerkickManager.Main.GameDataPlus();
          gdp.gd = gameTest.data;
          ltGameData.Add(gdp);

          iGames--;
          if (iGames <= 0) break;
        }

        bool bOk = mn.doGames(ltGameData, bContinuingTiming: true, bBackground: true);

        Assert.AreEqual(true, bOk);

        Utility.CollectPostGamesData(ltGameData, pgd);
      }

      // Stop stopwatch
      sw.Stop();

      Utility.PostGames(sTestResultLog, pgd, iPlayerSkillsH, iPlayerSkillsA, sw.ElapsedMilliseconds);

      /*
      int iGH = 0;
      int iGA = 0;
      int iV = 0;
      int iD = 0;
      int iL = 0;
      foreach (CornerkickManager.Main.GameDataPlus gdp in ltGameData) {
        iGH += gdp.gd.team[0].iGoals;
        iGA += gdp.gd.team[1].iGoals;

        if      (gdp.gd.team[0].iGoals > gdp.gd.team[1].iGoals) iV++;
        else if (gdp.gd.team[0].iGoals < gdp.gd.team[1].iGoals) iL++;
        else                                            iD++;
      }
      Debug.WriteLine("Total: " + iGH.ToString() + ":" + iGA.ToString());
      Debug.WriteLine("Avrge: " + (iGH / (float)nGames).ToString("0.00") + ":" + (iGA / (float)nGames).ToString("0.00"));
      Debug.WriteLine("Win Home/Draw/Away: " + iV.ToString() + "/" + iD.ToString() + "/" + iL.ToString());
      */
    }

    [TestMethod]
    public void TestMatchdays()
    {
      const bool bTestIO = false;
      const int iLand = 36;

      CornerkickManager.Main mn = new CornerkickManager.Main(bContinuingTime: true, iTrainingsPerDay: 2, iTrainingsPerDayMax: 3, bWorldCup: true);

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
      cup.iId = 2;
      cup.iId2 = iLand;
      cup.sName = "National Cup";
      cup.settings.iNeutral = 1;
      mn.ltCups.Add(cup);

      /////////////////////////////////////////////////////////////////////
      // Create league
      CornerkickManager.Cup league = new CornerkickManager.Cup(nGroups: 1, bGroupsTwoGames: true);
      league.iId = 1;
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
        clb.ltTactic[0].formation = mn.ltFormationen[8];

        // Add player to club
        Utility.addPlayerToClub(mn, ref clb);

        // Add stadium to club
        clb.stadium.setStadiumToDefault();
        clb.iAdmissionPrice = new int[3] { 10, 30, 100 };

        // Add sponsor to club
        mn.fz.addSponsor(clb, bForce: true);

        mn.ltClubs.Add(clb);

        cupInter.ltClubs[i / 4].Add(clb);
        cup.ltClubs[0].Add(clb);
        league.ltClubs[0].Add(clb);

        mn.doFormation(clb);

        // Put last player on transferlist
        mn.tr.putPlayerOnTransferlist(clb.ltPlayer[clb.ltPlayer.Count - 1].iId, 0);
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
      cupWc.settings.dtEnd = mn.dtSeasonEnd.AddDays(-1).Date + new TimeSpan(20, 00, 00);
      mn.ltCups.Add(cupWc);

      byte[] iNations = new byte[8] { 3, 13, 29, 30, 33, 36, 45, 54 };
      int iGroup = 0;
      foreach (byte iN in iNations) {
        CornerkickManager.Club clbNat = new CornerkickManager.Club();
        clbNat.bNation = true;
        clbNat.iId = mn.ltClubs.Count;
        clbNat.sName = CornerkickManager.Main.sLand[iN];
        clbNat.iLand = iN;
        clbNat.ltTactic[0].formation = mn.ltFormationen[8];

        List<CornerkickManager.Player> ltPlayerBest = mn.getBestPlayer(iN, clbNat.ltTactic[0].formation);
        while ((ltPlayerBest = mn.getBestPlayer(iN, clbNat.ltTactic[0].formation)).Count < 22) mn.plt.newPlayer(iNat: iN);
        //Assert.AreEqual(true, ltPlayerBest.Count >= 11);
        //clbNat.ltPlayer = ltPlayerBest;

        mn.ltClubs.Add(clbNat);

        mn.doFormation(clbNat);

        cupWc.ltClubs[iGroup / 4].Add(clbNat);
        iGroup++;
      }

      cupInter.settings.dtStart = dtLeagueStart.AddDays((int)((dtLeagueEnd - dtLeagueStart).TotalDays / 4.0)).Date + new TimeSpan(20, 45, 00);

      mn.calcMatchdays();

      mn.drawCup(cupInter);
      mn.drawCup(league);
      mn.drawCup(cupWc);

      cupInter.draw(mn.dtDatum);

      // Set biggest stadium to WC game data
      foreach (CornerkickManager.Cup.Matchday mdWc in cupWc.ltMatchdays) {
        if (mdWc.ltGameData == null) break;

        foreach (CornerkickGame.Game.Data gd in mdWc.ltGameData) {
          gd.stadium = mn.st.getBiggestStadium();
        }
      }

      // Test construction
      mn.ltClubs[0].buildings.iGround = 7;
      CornerkickManager.UI.doConstruction(mn.ltClubs[0], 0, 2, mn.dtDatum, "");
      CornerkickManager.UI.doConstruction(mn.ltClubs[0], 1, 2, mn.dtDatum, "");
      CornerkickManager.UI.doConstruction(mn.ltClubs[0], 2, 2, mn.dtDatum, "");
      CornerkickManager.UI.doConstruction(mn.ltClubs[0], 3, 2, mn.dtDatum, "");
      CornerkickManager.UI.doConstruction(mn.ltClubs[0], 4, 2, mn.dtDatum, "");
      CornerkickManager.UI.doConstruction(mn.ltClubs[0], 5, 2, mn.dtDatum, "");

      int iDayConstruct = 0;
      float iDaysConstruct1 = 180;
      float iDaysConstruct2 = 120;

      // Test player suspension
      CornerkickGame.Player plSusp = mn.ltClubs[0].ltPlayer[0];
      plSusp.iSuspension[0] = 5; // Suspend for 5 games

      Stopwatch sw = new Stopwatch();
      sw.Start();

      // Perform next step until end of season
      while (!CornerkickManager.Main.checkNextReturn(99, mn.next(bForce: true))) {
        if (mn.dtDatum.Hour == 0 && mn.dtDatum.Minute == 0) Debug.WriteLine(mn.dtDatum.ToString());

        // Set stadiums for semi-final and final games
        for (byte iMd = 3; iMd < 5; iMd++) {
          if (cupWc.ltMatchdays[iMd].ltGameData != null) {
            for (byte iGd = 0; iGd < cupWc.ltMatchdays[iMd].ltGameData.Count; iGd++) cupWc.ltMatchdays[iMd].ltGameData[iGd].stadium = mn.st.getBiggestStadium();
          }
        }

        // Test player suspension
        for (byte iS = 0; iS < 5; iS++) {
          if (mn.dtDatum.Equals(league.ltMatchdays[iS].dt.AddDays(1))) {
            Assert.AreEqual(5 - iS - 1, plSusp.iSuspension[0]);
          }
        }

        if (mn.dtDatum.Hour == 0 && mn.dtDatum.Minute == 0) {
          // Test constructions
          if (iDaysConstruct1 - iDayConstruct > 0) {
            Assert.AreEqual(mn.ltClubs[0].buildings.bgTrainingCourts.ctn.fDaysConstruct, iDaysConstruct1 - iDayConstruct, 0.001);
            Assert.AreEqual(mn.ltClubs[0].buildings.bgGym.ctn.fDaysConstruct, iDaysConstruct1 - iDayConstruct, 0.001);
            Assert.AreEqual(mn.ltClubs[0].buildings.bgSpa.ctn.fDaysConstruct, iDaysConstruct1 - iDayConstruct, 0.001);
            Assert.AreEqual(mn.ltClubs[0].buildings.bgClubHouse.ctn.fDaysConstruct, iDaysConstruct1 - iDayConstruct, 0.001);
            Assert.AreEqual(mn.ltClubs[0].buildings.bgClubMuseum.ctn.fDaysConstruct, iDaysConstruct1 - iDayConstruct, 0.001);

            iDaysConstruct1 = mn.ltClubs[0].buildings.bgGym.ctn.fDaysConstruct + iDayConstruct; // Reset for float precision
          } else {
            Assert.AreEqual(null, mn.ltClubs[0].buildings.bgTrainingCourts.ctn);
            Assert.AreEqual(null, mn.ltClubs[0].buildings.bgGym.ctn);
            Assert.AreEqual(null, mn.ltClubs[0].buildings.bgSpa.ctn);
            Assert.AreEqual(null, mn.ltClubs[0].buildings.bgClubHouse.ctn);
            Assert.AreEqual(null, mn.ltClubs[0].buildings.bgClubMuseum.ctn);
          }

          if (iDaysConstruct2 - iDayConstruct > 0) {
            Assert.AreEqual(mn.ltClubs[0].buildings.bgJouthInternat.ctn.fDaysConstruct, iDaysConstruct2 - iDayConstruct, 0.001);

            iDaysConstruct2 = mn.ltClubs[0].buildings.bgJouthInternat.ctn.fDaysConstruct + iDayConstruct; // Reset for float precision
          } else {
            Assert.AreEqual(null, mn.ltClubs[0].buildings.bgJouthInternat.ctn);
          }

          if ((int)mn.dtDatum.DayOfWeek > 0 && (int)mn.dtDatum.DayOfWeek < 6) { // ... on weekdays
            iDayConstruct++;
          }
        }
      }

      // Stop stopwatch
      sw.Stop();

      Debug.WriteLine("Start of season: " + mn.dtSeasonStart.ToString());
      foreach (CornerkickManager.Cup cupTmp in mn.ltCups) {
        Debug.WriteLine(cupTmp.sName);

        int iMd = 1;
        foreach (CornerkickManager.Cup.Matchday md in cupTmp.ltMatchdays) {
          if (md.ltGameData == null) break;

          Debug.WriteLine(iMd.ToString().PadLeft(2) + " - " + md.dt.ToString());
          foreach (CornerkickGame.Game.Data gd in md.ltGameData) {
            CornerkickManager.Club clbH = CornerkickManager.Tool.getClubFromId(gd.team[0].iTeamId, mn.ltClubs);
            CornerkickManager.Club clbA = CornerkickManager.Tool.getClubFromId(gd.team[1].iTeamId, mn.ltClubs);
            string sGame = "";
            if (clbH != null) sGame += clbH.sName.PadLeft(7);
            else sGame += " ? ";
            sGame += " - ";
            if (clbA != null) sGame += clbA.sName.PadLeft(7);
            else sGame += " ? ";
            string sResult = CornerkickManager.UI.getResultString(gd);
            if (!string.IsNullOrEmpty(sResult)) sGame += " - " + sResult;
            Debug.WriteLine("  " + sGame);

            // Test spectators
            Assert.AreEqual(true, gd.stadium.getSeats() > 0);
            Assert.AreEqual(true, gd.iSpectators[0] + gd.iSpectators[1] + gd.iSpectators[2] > 0);
          }

          if ((cupTmp.iId == 3 && iMd == 6) || cupTmp.iId == 7 && iMd == 3) {
            for (sbyte iG = 0; iG < cupTmp.settings.nGroups; iG++) {
              List<CornerkickManager.Cup.TableItem> tbl = cupTmp.getTable(iMd, iGroup: iG);
              Debug.WriteLine("+-----------------------+");
              Debug.WriteLine("|  Name  s u n  gls pts |");
              Debug.WriteLine("+-----------------------+");
              foreach (CornerkickManager.Cup.TableItem ti in tbl) {
                Debug.WriteLine("| " + ti.club.sName.PadRight(6) + " " + ti.iWDL[0].ToString() + " " + ti.iWDL[1].ToString() + " " + ti.iWDL[2].ToString() + " " + ti.iGoals.ToString().PadLeft(2) + ":" + ti.iGoalsOpp.ToString().PadRight(2) + " " + ti.iPoints.ToString().PadLeft(2) + " |");
              }
              Debug.WriteLine("+-----------------------+");
            }
          }

          iMd++;
        }
      }
      Debug.WriteLine("End of season: " + mn.dtSeasonEnd.ToString());

      // League
      List<CornerkickManager.Cup.TableItem> table = league.getTable();
      Assert.AreEqual(true, table.Count == nTeams);
      Assert.AreEqual(true, table[0].iGoals > 0);
      Assert.AreEqual(true, table[0].iWDL[0] > table[0].iWDL[2]);
      for (int i = 0; i < table.Count; i++) {
        Assert.AreEqual(i + 1, league.getPlace(table[i].club));
      }

      // Nat. cup
      Assert.AreEqual(true, cup.ltMatchdays.Count > 3);
      Assert.AreEqual(true, cup.ltMatchdays[3].ltGameData.Count == 1);

      // Intern. cup
      Assert.AreEqual(true, cupInter.ltMatchdays.Count > 10);
      Assert.AreEqual(true, cupInter.ltMatchdays[10].ltGameData.Count == 1);
      CornerkickManager.Club clbCupInter1 = null;
      CornerkickManager.Club clbCupInter2 = null;
      sbyte iWDD = CornerkickGame.Tool.getWinDrawDefeat(cupInter.ltMatchdays[10].ltGameData[0]);
      if (iWDD > 0) {
        clbCupInter1 = mn.ltClubs[cupInter.ltMatchdays[10].ltGameData[0].team[0].iTeamId];
        clbCupInter2 = mn.ltClubs[cupInter.ltMatchdays[10].ltGameData[0].team[1].iTeamId];
      } else if (iWDD < 0) {
        clbCupInter1 = mn.ltClubs[cupInter.ltMatchdays[10].ltGameData[0].team[1].iTeamId];
        clbCupInter2 = mn.ltClubs[cupInter.ltMatchdays[10].ltGameData[0].team[0].iTeamId];
      }
      Assert.AreEqual(1, cupInter.getPlace(clbCupInter1));
      Assert.AreEqual(2, cupInter.getPlace(clbCupInter2));

      // World cup
      Assert.AreEqual(true, cupWc.ltMatchdays.Count > 4);
      Assert.AreEqual(true, cupWc.ltMatchdays[4].ltGameData.Count == 1);
      Assert.AreEqual(true, cupWc.ltMatchdays[4].ltGameData[0].team[0].iGoals >= 0);
      Assert.AreEqual(true, cupWc.ltMatchdays[4].ltGameData[0].team[1].iGoals >= 0);
      Assert.AreEqual(true, cupWc.ltMatchdays[4].ltGameData[0].team[0].iGoals != cupWc.ltMatchdays[4].ltGameData[0].team[1].iGoals);

      Debug.WriteLine("Elapsed time: " + (sw.ElapsedMilliseconds / 1000.0).ToString("0.0") + " s");

      // save/load
      if (bTestIO) Utility.testIO(mn);
    }

    [TestMethod]
    public void TestGameWithPlayerInjury()
    {
      CornerkickManager.Main mn = new CornerkickManager.Main(bContinuingTime: true);

      CornerkickManager.Cup cup = new CornerkickManager.Cup(nGroups: 1, bGroupsTwoGames: true);
      cup.iId = 1;
      mn.ltCups.Add(cup);

      /////////////////////////////////////////////////////////////////////
      // Create Clubs
      int nTeams = 2;
      for (byte i = 0; i < nTeams; i++) {
        CornerkickManager.Club clb = new CornerkickManager.Club();

        clb.iId = i;
        clb.sName = "Team" + (i + 1).ToString();
        clb.ltTactic[0].formation = mn.ltFormationen[8];

        CornerkickManager.User usr = new CornerkickManager.User();
        clb.user = usr;
        usr.club = clb;
        mn.ltUser.Add(usr);

        // Add player to club
        Utility.addPlayerToClub(mn, ref clb);

        mn.doFormation(clb);

        mn.ltClubs.Add(clb);
        cup.ltClubs[0].Add(clb);
      }

      mn.calcMatchdays();
      mn.drawCup(cup);

      // Set injury to player at start
      mn.ltClubs[0].ltPlayer[0].injury = new CornerkickGame.Player.Injury();
      mn.ltClubs[0].ltPlayer[0].injury.fLength = 100f;
      mn.ltClubs[0].ltPlayer[0].injury.iLengthStart = 100;

      while (!CornerkickManager.Main.checkNextReturn(99, mn.next(bForce: true))) {
      }
    }

    [TestMethod]
    public void TestIO()
    {
      const bool bPerformCalendarSteps = true;

      CornerkickManager.Main mng = new CornerkickManager.Main(bContinuingTime: true, iWriteGamesToDisk: -1);
      mng.settings.sHomeDir = Path.Combine(mng.settings.sHomeDir, "io_test");
#if _ANSYS
      mng.settings.sHomeDir = @"D:\scratch\u522245\test\io_test";
#endif

      string sLoadFile = Path.Combine(mng.settings.sHomeDir, "test");
#if _ANSYS
      if (Directory.Exists(sLoadFile)) {
#else
      if (File.Exists(sLoadFile)) {
#endif
        mng.io.load(sLoadFile);

        if (bPerformCalendarSteps) {
          // Perform next step until end of season
          for (byte iSn = 0; iSn < 1; iSn++) {
            DateTime dtLoad = mng.dtDatum;

            mng.next();

            //while (mng.next() < 99) {
            //while (mng.dtDatum.Year == mng.dtSeasonStart.Year) {
            while ((mng.dtDatum - dtLoad).TotalDays < 30) {
              mng.next();

              if (mng.dtDatum.Hour == 0 && mng.dtDatum.Minute == 0) Debug.WriteLine(mng.dtDatum.ToString());

              if (mng.dtDatum.DayOfWeek == DayOfWeek.Saturday && mng.dtDatum.Hour == 15 && mng.dtDatum.Minute == 0) {
                foreach (CornerkickManager.Club clb in mng.ltClubs) {
                  mng.doFormation(clb);
                }
              }

              // Check that only one game per day
              Assert.AreEqual(false, Utility.testGameOnSameDay(mng));
            }

            mng.io.save(sLoadFile + "_2");

            // League
            CornerkickManager.Cup league = mng.tl.getCup(1, 36);
            if (league != null) {
              List<CornerkickManager.Cup.TableItem> table = league.getTable();
              Assert.AreEqual(true, table[0].iGoals > 0);
              Assert.AreEqual(true, table[0].iWDL[0] > table[0].iWDL[2]);

              // Print table
              StreamWriter swTbl = new StreamWriter(Path.Combine(mng.settings.sHomeDir, "Table_" + iSn.ToString() + ".txt"), false);
              foreach (string sLine in CornerkickManager.UI.getTable(table)) swTbl.WriteLine(sLine);
              swTbl.Close();
            }

            foreach (CornerkickManager.Cup cpNat in mng.ltCups) {
              if (cpNat.iId == 2) {
                int iRound = 3;
                if (cpNat.iId2 == 36) iRound = 4;
                Assert.AreEqual(true, cpNat.ltMatchdays.Count > 3);
                Assert.AreEqual(1, cpNat.ltMatchdays[iRound].ltGameData.Count);
              }
            }

            CornerkickManager.Cup cpGold = mng.tl.getCup(3);
            Assert.AreEqual(true, cpGold.ltMatchdays.Count > 10);
            Assert.AreEqual(1, cpGold.ltMatchdays[12].ltGameData.Count);
            Assert.AreEqual(true, cpGold.ltMatchdays[12].ltGameData[0].team[0].iGoals >= 0);
            Assert.AreEqual(true, cpGold.ltMatchdays[12].ltGameData[0].team[1].iGoals >= 0);
            Assert.AreEqual(true, cpGold.ltMatchdays[12].ltGameData[0].team[0].iGoals != cpGold.ltMatchdays[12].ltGameData[0].team[1].iGoals);

            // World cup
            CornerkickManager.Cup cupWc = mng.tl.getCup(7);
            Assert.AreEqual(true, cupWc.ltMatchdays.Count > 4);
            Assert.AreEqual(1, cupWc.ltMatchdays[4].ltGameData.Count);
            Assert.AreEqual(true, cupWc.ltMatchdays[4].ltGameData[0].team[0].iGoals >= 0);
            Assert.AreEqual(true, cupWc.ltMatchdays[4].ltGameData[0].team[1].iGoals >= 0);
            Assert.AreEqual(true, cupWc.ltMatchdays[4].ltGameData[0].team[0].iGoals != cupWc.ltMatchdays[4].ltGameData[0].team[1].iGoals);
          }
        }

        Utility.testIO(mng);
      }
    }

    [TestMethod]
    public void TestIOGameData()
    {
      CornerkickManager.Main mn = new CornerkickManager.Main();
      mn.settings.sHomeDir = Path.Combine(mn.settings.sHomeDir, "test");
#if _ANSYS
      mn.settings.sHomeDir = @"D:\\scratch\\u522245\\test";
#endif
      if (!Directory.Exists(mn.settings.sHomeDir)) Directory.CreateDirectory(mn.settings.sHomeDir);

      CornerkickGame.Game gameTest = game.tl.getDefaultGame();
      for (byte iHA = 0; iHA < 2; iHA++) {
        for (byte iPl = 0; iPl < gameTest.player[iHA].Length; iPl++) {
          CornerkickManager.Player plMng = new CornerkickManager.Player() { plGame = gameTest.player[iHA][iPl] };
          mn.ltPlayer.Add(plMng);
        }
      }

      int[] iSeats = new int[3];
      for (byte iS = 0; iS < iSeats.Length; iS++) iSeats[iS] = gameTest.data.stadium.getSeats(iS);

      // Perform the game
      bool bOk = mn.doGame(gameTest, bAlwaysWriteToDisk: true, bWaitUntilGameIsSaved: true);
      Assert.AreEqual(true, bOk);

      // Get game files
      DirectoryInfo diGames = new DirectoryInfo(@Path.Combine(mn.settings.sHomeDir, "save", "games"));
      FileInfo[] fiGames = diGames.GetFiles("*.ckgx");
      while (fiGames.Length == 0) {
        fiGames = diGames.GetFiles("*.ckgx");
      }

      // Read game file(s) and perform checks
      foreach (FileInfo fiGame in fiGames) {
        CornerkickGame.Game gameLoad = mn.io.loadGame(fiGame.FullName);

        for (byte iS = 0; iS < iSeats.Length; iS++) Assert.AreEqual(iSeats[iS], gameTest.data.stadium.getSeats(iS));

        List<CornerkickGame.Game.Shoot> ltShoots = CornerkickManager.UI.getShoots(gameLoad.data.ltState);
        foreach (CornerkickGame.Game.Shoot shoot in ltShoots) {
          float fChanceShootOnGoal = gameLoad.ai.getChanceShootOnGoal(shoot.plShoot, 0);
          Assert.AreEqual(shoot.fChanceOnGoal, fChanceShootOnGoal, 0.01);
        }

        Directory.Delete(mn.settings.sHomeDir, true);
      }
    }

    [TestMethod]
    public void TestNewPlayerFoot()
    {
      CornerkickManager.Main mn = new CornerkickManager.Main();
      const int nPl = 5000;

      byte[] iPosLeft = new byte[] { 3, 6, 9 };
      foreach (byte iPos in iPosLeft) {
        int iPlLeft = 0;

        for (int iP = 0; iP < nPl; iP++) {
          CornerkickManager.Player plNew = mn.plt.newPlayer(iPos: iPos);
          if (plNew.plGame.fFootL > 0.9999f) iPlLeft++;
        }

        Assert.AreEqual(0.8, iPlLeft / (double)nPl, 0.015);
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

          CornerkickGame.Player plDef = gameTest.player[iHA][1];
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
            else iDuelA++;
          }
        }
      }

      Assert.AreEqual(0.966185451, iDuelH / (double)nDuels, 0.01);
      Assert.AreEqual(0.966185451, iDuelA / (double)nDuels, 0.01);
    }

    [TestMethod]
    public void TestPenaltySuccessRate()
    {
      const int nPenalties = 10000;

      for (byte iHA = 0; iHA < 2; iHA++) {
        int iG = 0;
        for (int iP = 0; iP < nPenalties; iP++) {
          CornerkickGame.Game gameTest = game.tl.getDefaultGame(iPlayerSkills: 8);
          gameTest.next();
          gameTest.iStandard = 0;

          CornerkickGame.Player plDef = gameTest.player[1 - iHA][1];
          CornerkickGame.Player plOff = gameTest.player[iHA][10];

          if (iHA == 0) plDef.ptPos = new Point(gameTest.ptPitch.X - (gameTest.ptBox.X / 2), 0);
          else plDef.ptPos = new Point(gameTest.ptBox.X / 2, 0);
          plOff.ptPos = plDef.ptPos;
          if (iHA == 0) plOff.ptPos.X += 2;
          else plOff.ptPos.X -= 2;

          gameTest.ball.ptPos = plOff.ptPos;
          gameTest.ball.plAtBall = plOff;

          gameTest.doDuel(plDef, 3);

          // Check that standard is penalty
          Assert.AreEqual(1, Math.Abs(gameTest.iStandard));

          while (gameTest.iStandardCounter > 0) {
            gameTest.next();
          }

          // Finalize shoot
          gameTest.next();

          // Check that penalty is not set anymore
          Assert.AreEqual(false, Math.Abs(gameTest.iStandard) == 1);

          iG += gameTest.data.team[iHA].iGoals;
        }

        // Check penalty success (79% .. 81% .. 83%)
        Assert.AreEqual(0.81, iG / (float)nPenalties, 0.02);
      }
    }
  }

  internal class Utility
  {
    private static CornerkickGame.Game game = new CornerkickGame.Game(new CornerkickGame.Game.Data());

    internal static void testIO(CornerkickManager.Main mng)
    {
      string sSaveFile = Path.Combine(mng.settings.sHomeDir, "unittest_save");

      DateTime dtCurrent = mng.dtDatum;
      int iSeasonCountTmp = mng.iSeason;
      float fInterest = mng.fz.fGlobalCreditInterest;

      List<string> ltUserId = new List<string>();
      foreach (CornerkickManager.User user in mng.ltUser) {
        ltUserId.Add(user.id);
      }

      List<string> ltPlayerName = new List<string>();
      foreach (CornerkickManager.Player pl in mng.ltPlayer) {
        ltPlayerName.Add(pl.sName);
      }

      mng.io.save(sSaveFile);
      mng = new CornerkickManager.Main();
      mng.io.load(sSaveFile);

      // General
      Assert.AreEqual(dtCurrent, mng.dtDatum);
      Assert.AreEqual(iSeasonCountTmp, mng.iSeason);
      Assert.AreEqual(fInterest, mng.fz.fGlobalCreditInterest);

      // User id
      Assert.AreEqual(ltUserId.Count, mng.ltUser.Count);
      for (int iU = 0; iU < ltUserId.Count; iU++) {
        Assert.AreEqual(ltUserId[iU], mng.ltUser[iU].id);
      }

      // Player name
      Assert.AreEqual(ltPlayerName.Count, mng.ltPlayer.Count);
      for (int iPl = 0; iPl < ltPlayerName.Count; iPl++) {
        Assert.AreEqual(ltPlayerName[iPl], mng.ltPlayer[iPl].sName);
      }
    }

    internal static bool testGameOnSameDay(CornerkickManager.Main mng)
    {
      foreach (CornerkickManager.Club clb in mng.ltClubs) {
        bool bGame = false;

        foreach (CornerkickManager.Cup cp in mng.ltCups) {
          foreach (CornerkickManager.Cup.Matchday md in cp.ltMatchdays) {
            if (md.ltGameData == null) continue;

            foreach (CornerkickGame.Game.Data gd in md.ltGameData) {
              if (mng.dtDatum.Equals(gd.dt)) {
                if (clb.iId == gd.team[0].iTeamId ||
                    clb.iId == gd.team[1].iTeamId) {
                  if (bGame) return true;

                  bGame = true;
                  break;
                }
              }
            }
          }
        }
      }

      return false;
    }

    internal static void addPlayerToClub(CornerkickManager.Main mn, ref CornerkickManager.Club clb)
    {
      for (byte i = 0; i < 2; i++) {
        for (byte iP = 1; iP < 12; iP++) {
          CornerkickManager.Player pl = mn.plt.newPlayer(club: clb, iPos: iP);
          pl.iNr = (byte)(iP * (i + 1));
        }
      }
    }

    internal static void testGoal(CornerkickGame.Game gameTest, bool bHome, int iGoalH, int iGoalA)
    {
      if (bHome) testGoal(gameTest, 0, iGoalH, iGoalA);
      else       testGoal(gameTest, 1, iGoalH, iGoalA);
    }
    internal static void testGoal(CornerkickGame.Game gameTest, byte iHA, int iGoalH, int iGoalA)
    {
      if (iHA == 0) {
        if (iGoalH + 1 != gameTest.data.team[0].iGoals) {
          while (gameTest.iStandardCounter > 0) {
            gameTest.next();
          }
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
        }
        Assert.AreEqual(iGoalH,     gameTest.data.team[0].iGoals);
        Assert.AreEqual(iGoalA + 1, gameTest.data.team[1].iGoals);
      }

      if (iHA == 0) Assert.AreEqual(gameTest.ptPitch.X + 1, gameTest.ball.ptPos.X, "Ball is not in away goal!");
      else          Assert.AreEqual(-1,                     gameTest.ball.ptPos.X, "Ball is not in home goal!");
      Assert.AreEqual(5, Math.Abs(gameTest.iStandard), "Standard is not kick-off!");

      gameTest.next();
      while (gameTest.iStandardCounter > 0) {
        Assert.AreEqual(gameTest.ptPitch.X / 2, gameTest.ball.ptPos.X, "Ball is not in middle-point!");
        Assert.AreEqual(0, gameTest.ball.ptPos.Y, "Ball is not in middle-point!");
        gameTest.next();
      }
    }

    internal static void PostGamesHeader(string sTestResultLog, DateTime dtStart, int nGames, int iPlayerSkillsH, int iPlayerSkillsA)
    {
      StreamWriter swLog = new StreamWriter(sTestResultLog, true);

      swLog.WriteLine("");
      swLog.WriteLine("##################################################################################################");
      swLog.WriteLine("### Start date: " + dtStart + ". Performed games: " + nGames.ToString() + ", player skill (H/A): " + iPlayerSkillsH.ToString() + "/" + iPlayerSkillsA.ToString());
      swLog.Close();
    }

    internal static float getPlayerActionTotal(float[] fAction)
    {
      float fTotal = 0f;
      foreach (float f in fAction) fTotal += f;

      return fTotal;
    }

    internal static CornerkickGame.Game switchTeams(CornerkickGame.Game game0)
    {
      CornerkickGame.Player[][] pl1 = new CornerkickGame.Player[2][];

      for (byte iHA = 0; iHA < 2; iHA++) {
        pl1[iHA] = new CornerkickGame.Player[game0.player[iHA].Length];
        for (byte iP = 0; iP < game0.data.nPlStart; iP++) {
          pl1[iHA][iP] = game0.player[iHA][iP].Clone(true);
        }
      }

      CornerkickGame.Game.Data gd0 = game0.data.Clone(true);
      CornerkickGame.Game game1 = new CornerkickGame.Game(gd0, pl1);
      game1.iStandard = game0.iStandard;
      game1.data.bInjuriesPossible = game0.data.bInjuriesPossible;
      game1.data.bCardsPossible = game0.data.bCardsPossible;

      for (byte iP = 0; iP < game.data.nPlStart; iP++) {
        // Position
        Point ptPosH = game0.player[0][iP].ptPos;
        game1.player[0][iP].ptPos = CornerkickGame.Tool.transformPosition(game0.player[1][iP].ptPos, game0.ptPitch.X);
        game1.player[1][iP].ptPos = CornerkickGame.Tool.transformPosition(ptPosH, game0.ptPitch.X);

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
        /*
        float fSA = game0.player[0][iP].fStepsAll;
        game1.player[0][iP].fStepsAll = game1.player[1][iP].fStepsAll;
        game1.player[1][iP].fStepsAll = fSA;
        */
      }

      // Ball
      game1.ball.ptPos = CornerkickGame.Tool.transformPosition(game0.ball.ptPos, game0.ptPitch.X);
      game1.ball.ptPosLast = CornerkickGame.Tool.transformPosition(game0.ball.ptPosLast, game0.ptPitch.X);
      if (game0.ball.plAtBall != null) game1.ball.plAtBall = game1.player[1 - game0.ball.plAtBall.iHA][game0.ball.plAtBall.iIndex];
      if (game0.ball.plAtBallLast != null) game1.ball.plAtBallLast = game1.player[1 - game0.ball.plAtBallLast.iHA][game0.ball.plAtBallLast.iIndex];
      game1.ball.nPassSteps = game0.ball.nPassSteps;

      return game1;
    }

    internal static void testHA(CornerkickGame.Game game0)
    {
      CornerkickGame.Game game1 = switchTeams(game0);

      if (game0.ball.plAtBall != null) {
        CornerkickGame.Player plClosestOpp0 = game0.tl.getClosestPlayer(game0.ball.plAtBall, game0.ball.plAtBall.iHA == 1);
        int iDistClosestOpp0 = game0.tl.getDistancePlayerSteps(game0.ball.plAtBall, plClosestOpp0);
        CornerkickGame.Player plClosestOpp1 = game1.tl.getClosestPlayer(game1.ball.plAtBall, game1.ball.plAtBall.iHA == 1);
        int iDistClosestOpp1 = game1.tl.getDistancePlayerSteps(game1.ball.plAtBall, plClosestOpp1);

#if !_AI2
        // Shoot
        double fShootChance0 = game0.ai.getChanceShoot(game0.ball.plAtBall, game0.data.team[0].ltTactic[0], game0.ball.nPassSteps > 0, Math.Abs(game0.iStandard) == 2);
        double fShootChance1 = game1.ai.getChanceShoot(game1.ball.plAtBall, game1.data.team[1].ltTactic[0], game1.ball.nPassSteps > 0, Math.Abs(game1.iStandard) == 2);
        if (fShootChance0 != fShootChance1) {
          fShootChance0 = game0.ai.getChanceShoot(game0.ball.plAtBall, game0.data.team[0].ltTactic[0], game0.ball.nPassSteps > 0, Math.Abs(game0.iStandard) == 2);
          fShootChance1 = game1.ai.getChanceShoot(game1.ball.plAtBall, game1.data.team[1].ltTactic[0], game1.ball.nPassSteps > 0, Math.Abs(game1.iStandard) == 2);

          fShootChance0 = game0.ai.getChanceShoot(game0.ball.plAtBall, game0.data.team[0].ltTactic[0], game0.ball.nPassSteps > 0, Math.Abs(game0.iStandard) == 2);
          fShootChance1 = game1.ai.getChanceShoot(game1.ball.plAtBall, game1.data.team[1].ltTactic[0], game1.ball.nPassSteps > 0, Math.Abs(game1.iStandard) == 2);

          fShootChance0 = game0.ai.getChanceShoot(game0.ball.plAtBall, game0.data.team[0].ltTactic[0], game0.ball.nPassSteps > 0, Math.Abs(game0.iStandard) == 2);
          fShootChance1 = game1.ai.getChanceShoot(game1.ball.plAtBall, game1.data.team[1].ltTactic[0], game1.ball.nPassSteps > 0, Math.Abs(game1.iStandard) == 2);
        }
        Assert.AreEqual(fShootChance0, fShootChance1, 0.0001);
#endif

        // Pass
#if !_AI2
        int iDummy1 = 0;
        int iDummy2 = 0;
        double fPassChance0 = game0.ai.getChancePass(game0.ball.plAtBall, iDistClosestOpp0, game0.data.team[0].ltTactic[0].fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);
        double fPassChance1 = game1.ai.getChancePass(game1.ball.plAtBall, iDistClosestOpp1, game1.data.team[1].ltTactic[0].fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);
        if (fPassChance0 != fPassChance1) {
          fPassChance0 = game0.ai.getChancePass(game0.ball.plAtBall, iDistClosestOpp0, game0.data.team[0].ltTactic[0].fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);
          fPassChance1 = game1.ai.getChancePass(game1.ball.plAtBall, iDistClosestOpp1, game1.data.team[1].ltTactic[0].fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);

          fPassChance0 = game0.ai.getChancePass(game0.ball.plAtBall, iDistClosestOpp0, game0.data.team[0].ltTactic[0].fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);
          fPassChance1 = game1.ai.getChancePass(game1.ball.plAtBall, iDistClosestOpp1, game1.data.team[1].ltTactic[0].fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);

          fPassChance0 = game0.ai.getChancePass(game0.ball.plAtBall, iDistClosestOpp0, game0.data.team[0].ltTactic[0].fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);
          fPassChance1 = game1.ai.getChancePass(game1.ball.plAtBall, iDistClosestOpp1, game1.data.team[1].ltTactic[0].fPassFreq, out plDummy, out iDummy1, out iDummy2, 0);
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
        float[] fAction0;
        sbyte iAction0 = game0.ai.getPlayerAction(game0.ball.plAtBall, out fAction0, false, 0);
        float[] fAction1;
        sbyte iAction1 = game1.ai.getPlayerAction(game1.ball.plAtBall, out fAction1, false, 0);
        for (byte iA = 0; iA < fAction0.Length; iA++) {
          //Assert.AreEqual(fAction0[iA], fAction1[iA], 0.0001);
        }
      }
    }

    internal class PostGamesData
    {
      internal int nGames = 0;
      internal int iGH = 0;
      internal int iGA = 0;
      internal int iShootsH = 0;
      internal int iShootsA = 0;
      internal double fChanceGoalH = 0.0;
      internal double fChanceGoalA = 0.0;
      internal double fShootDistH = 0.0;
      internal double fShootDistA = 0.0;
      internal int[] iShootRange = new int[8];
      internal int[] iShootRangeGoals = new int[8];
      internal int iV = 0;
      internal int iD = 0;
      internal int iL = 0;
      internal uint iStepsH = 0;
      internal uint iStepsA = 0;
      internal uint iDuelH = 0;
      internal uint iDuelA = 0;
      internal uint iCardYellowH = 0;
      internal uint iCardYellowA = 0;
      internal uint iCardYelRedH = 0;
      internal uint iCardYelRedA = 0;
      internal uint iCardRedH = 0;
      internal uint iCardRedA = 0;
      internal uint iPossH = 0;
      internal uint iPossA = 0;
      internal uint iPassH = 0;
      internal uint iPassA = 0;
      internal uint iOffsiteH = 0;
      internal uint iOffsiteA = 0;
      internal double[] fGrade = new double[11]; // Average grade depending on position
      internal int[] iGradeDist = new int[6]; // Distribution of grades
      internal int[] iGamesGrd;
      internal int[][][] iScorerField = new int[2][][]; // Scorer counter dependend on pitch position [shooter/assist][X][Y]
      internal int iAssists = 0;
      internal double fFreshDrop = 0.0;
      internal double fFreshDropHt = 0.0;
      internal int iInjuries = 0;
      internal int iInjuriesPlayer = 0;

      internal PostGamesData()
      {
        for (int j = 0; j < iScorerField.Length; j++) {
          iScorerField[j] = new int[6][];
          for (int jj = 0; jj < iScorerField[j].Length; jj++) iScorerField[j][jj] = new int[5];
        }

        iGamesGrd = new int[fGrade.Length];
      }
    }
    internal static void CollectPostGamesData(List<CornerkickManager.Main.GameDataPlus> ltGameDataPlus, PostGamesData pgd)
    {
      List<CornerkickGame.Game.Data> ltGameData = new List<CornerkickGame.Game.Data>();
      foreach (CornerkickManager.Main.GameDataPlus gdp in ltGameDataPlus) ltGameData.Add(gdp.gd);

      CollectPostGamesData(ltGameData, pgd);
    }
    private static void CollectPostGamesData(List<CornerkickGame.Game.Data> ltGameData, PostGamesData pgd)
    {
      foreach (CornerkickGame.Game.Data gd in ltGameData) CollectPostGamesData(gd, pgd);
    }
    internal static void CollectPostGamesData(CornerkickGame.Game.Data gd, PostGamesData pgd)
    {
      pgd.nGames++;

      CornerkickGame.Game gameDefault = game.tl.getDefaultGame();

      // Get ave. freshness pre-game
      double fFreshAvePre = 0.0;
      for (byte iHA = 0; iHA < 2; iHA++) {
        for (byte iPl = 0; iPl < gd.nPlStart; iPl++) {
          fFreshAvePre += gd.ltState[0].player[iHA][iPl].fFresh;
        }
      }
      fFreshAvePre /= (2 * gd.nPlStart);

      bool bHalfTime = false;
      foreach (CornerkickGame.Game.State state in gd.ltState) {
        if (!bHalfTime) {
          foreach (CornerkickGame.Game.Comment ct in state.ltComment) {
            if (ct.sText.Contains("Ende der ersten Halbzeit")) {
              // Get ave. freshness half-time
              double fFreshAvePostHt = 0.0;
              for (byte iHA = 0; iHA < 2; iHA++) {
                for (byte iPl = 0; iPl < gd.nPlStart; iPl++) {
                  fFreshAvePostHt += state.player[iHA][iPl].fFresh;
                }
              }
              fFreshAvePostHt /= (2 * gd.nPlStart);
              pgd.fFreshDropHt += fFreshAvePre - fFreshAvePostHt;

              bHalfTime = true;

              break;
            }
          }
        }

        CornerkickGame.Game.Shoot shoot = state.shoot;

        // Count scorer position on field
        if (shoot.iResult == 1 && shoot.bFinished) {
          for (byte jj = 0; jj < 2; jj++) {
            CornerkickGame.Player plField = shoot.plShoot;
            if (jj > 0) plField = shoot.plAssist;

            if (plField == null) continue;

            Point ptFieldPos = CornerkickGame.Tool.transformPosition(plField.ptPos, gameDefault.ptPitch.X, plField.iHA == 1);
            int iFieldPosX = ptFieldPos.X / 10;
            if (iFieldPosX >= pgd.iScorerField[0].Length) iFieldPosX = pgd.iScorerField[0].Length - 1;

            if (iFieldPosX >= 0) {
              int iFieldPosY = 2;
              if (ptFieldPos.Y <= -gameDefault.ptPitch.Y * (4f / 5f)) iFieldPosY = 0;
              else if (ptFieldPos.Y <= -gameDefault.ptPitch.Y * (2f / 5f)) iFieldPosY = 1;
              else if (ptFieldPos.Y >= +gameDefault.ptPitch.Y * (4f / 5f)) iFieldPosY = 4;
              else if (ptFieldPos.Y >= +gameDefault.ptPitch.Y * (2f / 5f)) iFieldPosY = 3;

              pgd.iScorerField[jj][iFieldPosX][iFieldPosY]++;
            }

            if (jj > 0) pgd.iAssists++;
          }
        }
      } // for each state

      CornerkickGame.Game.State stateLast = gd.ltState[gd.ltState.Count - 1];

      // Get ave. freshness post-game
      double fFreshAvePost = 0.0;
      for (byte iHA = 0; iHA < 2; iHA++) {
        for (byte iPl = 0; iPl < gd.nPlStart; iPl++) {
          fFreshAvePost += stateLast.player[iHA][iPl].fFresh;
        }
      }
      fFreshAvePost /= (2 * gd.nPlStart);
      pgd.fFreshDrop += fFreshAvePre - fFreshAvePost;

      // Count data
      pgd.iGH += gd.team[0].iGoals;
      pgd.iGA += gd.team[1].iGoals;

      List<CornerkickGame.Game.Shoot> ltShootsH = CornerkickManager.UI.getShoots(gd.ltState, 0);
      pgd.iShootsH += ltShootsH.Count;
      foreach (CornerkickGame.Game.Shoot shoot in ltShootsH) {
        float fShootDistTmp = CornerkickGame.Tool.getDistanceToGoal(shoot.plShoot, gameDefault.ptPitch.X, gameDefault.fConvertDist2Meter)[0];
        pgd.fChanceGoalH += CornerkickGame.AI.getChanceShootGoal(shoot);
        pgd.fShootDistH += fShootDistTmp;

        addShootToRange(fShootDistTmp, ref pgd.iShootRange, shoot.iResult, ref pgd.iShootRangeGoals);
      }

      List<CornerkickGame.Game.Shoot> ltShootsA = CornerkickManager.UI.getShoots(gd.ltState, 1);
      pgd.iShootsA += ltShootsA.Count;
      foreach (CornerkickGame.Game.Shoot shoot in ltShootsA) {
        float fShootDistTmp = CornerkickGame.Tool.getDistanceToGoal(shoot.plShoot, gameDefault.ptPitch.X, gameDefault.fConvertDist2Meter)[0];
        pgd.fChanceGoalA += CornerkickGame.AI.getChanceShootGoal(shoot);
        pgd.fShootDistA += fShootDistTmp;

        addShootToRange(fShootDistTmp, ref pgd.iShootRange, shoot.iResult, ref pgd.iShootRangeGoals);
      }

      // Count duels
      pgd.iDuelH += (uint)CornerkickManager.UI.getDuels(gd.ltState, 0).Count;
      pgd.iDuelA += (uint)CornerkickManager.UI.getDuels(gd.ltState, 1).Count;

      // Count cards
      List<CornerkickGame.Game.Duel> ltCards = CornerkickManager.UI.getCards(gd.ltState);
      foreach (CornerkickGame.Game.Duel crd in ltCards) {
        if (crd.plDef.iHA == 0) {
          if (crd.iResult == 3) pgd.iCardYellowH++;
          else if (crd.iResult == 4) pgd.iCardYelRedH++;
          else if (crd.iResult == 5) pgd.iCardRedH++;
        } else if (crd.plDef.iHA == 1) {
          if (crd.iResult == 3) pgd.iCardYellowA++;
          else if (crd.iResult == 4) pgd.iCardYelRedA++;
          else if (crd.iResult == 5) pgd.iCardRedA++;
        }
      }

      // Count steps
      for (byte iPl = 0; iPl < stateLast.player[0].Length; iPl++) pgd.iStepsH += (uint)stateLast.player[0][iPl].iSteps;
      for (byte iPl = 0; iPl < stateLast.player[1].Length; iPl++) pgd.iStepsA += (uint)stateLast.player[1][iPl].iSteps;

      // Count possession
      pgd.iPossH += (uint)gd.team[0].iPossession;
      pgd.iPossA += (uint)gd.team[1].iPossession;

      // Count passes
      List<CornerkickGame.Game.Pass> lPassesH = CornerkickManager.UI.getPasses(gd.ltState, 0);
      pgd.iPassH += (uint)lPassesH.Count;
      List<CornerkickGame.Game.Pass> lPassesA = CornerkickManager.UI.getPasses(gd.ltState, 1);
      pgd.iPassA += (uint)lPassesA.Count;

      // Count offsites
      pgd.iOffsiteH += (uint)gd.team[0].iOffsite;
      pgd.iOffsiteA += (uint)gd.team[1].iOffsite;

      // Player grade
      double[] fGradeTeamAve = new double[pgd.fGrade.Length];
      int[] iPlG = new int[fGradeTeamAve.Length];
      for (byte iHA = 0; iHA < 2; iHA++) {
        foreach (CornerkickGame.Player plG in stateLast.player[iHA]) {
          byte iPosGrd = CornerkickGame.Tool.getBasisPos(gameDefault.tl.getPosRole(plG));
          float fGrd = plG.getGrade(iPosGrd, 90);
          if (fGrd > 0f) {
            fGradeTeamAve[iPosGrd - 1] += plG.getGrade(iPosGrd, 90);
            pgd.iGradeDist[(int)(fGrd - 1f)]++;
            iPlG[iPosGrd - 1]++;
          }
        }
      }
      for (byte iGrd = 0; iGrd < pgd.fGrade.Length; iGrd++) {
        if (iPlG[iGrd] > 0) {
          pgd.fGrade[iGrd] += fGradeTeamAve[iGrd] / iPlG[iGrd];
          pgd.iGamesGrd[iGrd]++;
        }
      }

      if (gd.team[0].iGoals > gd.team[1].iGoals) pgd.iV++;
      else if (gd.team[0].iGoals < gd.team[1].iGoals) pgd.iL++;
      else pgd.iD++;

      // Get injuries
      for (byte iHA = 0; iHA < 2; iHA++) {
        for (byte iPl = 0; iPl < stateLast.player[iHA].Length; iPl++) {
          if (stateLast.player[iHA][iPl] == null) continue;

          if (stateLast.player[iHA][iPl].injury != null) pgd.iInjuries++;
          pgd.iInjuriesPlayer++;
        }
      }
    }

    internal static void PostGames(string sTestResultLog, PostGamesData pgd, byte iPlayerSkillsH, byte iPlayerSkillsA, long iElapsedMilliseconds)
    {
      pgd.fShootDistH /= pgd.iShootsH;
      pgd.fShootDistA /= pgd.iShootsA;

      for (byte iGrd = 0; iGrd < pgd.fGrade.Length; iGrd++) {
        if (pgd.iGamesGrd[iGrd] > 0) pgd.fGrade[iGrd] /= pgd.iGamesGrd[iGrd];
      }

      Debug.WriteLine("");
      Debug.WriteLine("OOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO");
      Debug.WriteLine("OOO        Statistics        OOO");
      Debug.WriteLine("OOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO");

      Trace.Listeners.Add(new TextWriterTraceListener(sTestResultLog));
      Trace.AutoFlush = true;
      Trace.Indent();

      // Assist field
      Trace.WriteLine("Goals/Assists field:");
      for (int jY = 0; jY < pgd.iScorerField[0][0].Length; jY++) { // for each Y
        for (int jX = 0; jX < pgd.iScorerField[0].Length; jX++) { // for each X
          Trace.Write((pgd.iScorerField[0][jX][jY] / (float)(pgd.iGH + pgd.iGA)).ToString(" 00.00%") + "/" + (pgd.iScorerField[1][jX][jY] / (float)pgd.iAssists).ToString("00.00% "));
        }
        Trace.WriteLine("");
      }
      Trace.WriteLine("");

      Trace.WriteLine("                Ave.  /  H/A");
      Trace.WriteLine("        goals: " + ((pgd.iGH + pgd.iGA) / (2.0 * pgd.nGames)).ToString("0.0000").PadLeft(6) + " / " + (pgd.iGH / (double)pgd.iGA).ToString("0.0000"));
      Trace.WriteLine("  chance goal: " + ((pgd.fChanceGoalH + pgd.fChanceGoalA) / (2.0 * pgd.nGames)).ToString("0.0000").PadLeft(6) + " / " + (pgd.fChanceGoalH / pgd.fChanceGoalA).ToString("0.0000"));
      Trace.WriteLine("       shoots: " + ((pgd.iShootsH + pgd.iShootsA) / (2.0 * pgd.nGames)).ToString("0.0000").PadLeft(6) + " / " + (pgd.iShootsH / (double)pgd.iShootsA).ToString("0.0000"));
      Trace.WriteLine("  shoot dist.: " + ((pgd.fShootDistH + pgd.fShootDistA) / (2.0)).ToString("0.000").PadLeft(6) + " / " + (pgd.fShootDistH / pgd.fShootDistA).ToString("0.0000"));
      Trace.WriteLine("        duels: " + ((pgd.iDuelH + pgd.iDuelA) / (2.0 * pgd.nGames)).ToString("0.000").PadLeft(6) + " / " + (pgd.iDuelH / (double)pgd.iDuelA).ToString("0.0000"));
      Trace.WriteLine(" yellow cards: " + ((pgd.iCardYellowH + pgd.iCardYellowA) / (2.0 * pgd.nGames)).ToString("0.000").PadLeft(6) + " / " + (pgd.iCardYellowH / (double)pgd.iCardYellowA).ToString("0.0000"));
      Trace.WriteLine("    y/r cards: " + ((pgd.iCardYelRedH + pgd.iCardYelRedA) / (2.0 * pgd.nGames)).ToString("0.000").PadLeft(6) + " / " + (pgd.iCardYelRedH / (double)pgd.iCardYelRedA).ToString("0.0000"));
      Trace.WriteLine("    red cards: " + ((pgd.iCardRedH + pgd.iCardRedA) / (2.0 * pgd.nGames)).ToString("0.000").PadLeft(6) + " / " + (pgd.iCardRedH / (double)pgd.iCardRedA).ToString("0.0000"));
      Trace.WriteLine("        steps: " + ((pgd.iStepsH + pgd.iStepsA) / (2.0 * pgd.nGames)).ToString("0 ").PadLeft(6) + " / " + (pgd.iStepsH / (double)pgd.iStepsA).ToString("0.0000"));
      Trace.WriteLine("   possession: " + ((pgd.iPossH + pgd.iPossA) / (2.0 * pgd.nGames)).ToString("0.0").PadLeft(6) + " / " + (pgd.iPossH / (double)pgd.iPossA).ToString("0.0000"));
      Trace.WriteLine("       passes: " + ((pgd.iPassH + pgd.iPassA) / (2.0 * pgd.nGames)).ToString("0.00").PadLeft(6) + " / " + (pgd.iPassH / (double)pgd.iPassA).ToString("0.0000"));
      Trace.WriteLine("     offsites: " + ((pgd.iOffsiteH + pgd.iOffsiteA) / (2.0 * pgd.nGames)).ToString("0.0000").PadLeft(6) + " / " + (pgd.iOffsiteH / (double)pgd.iOffsiteA).ToString("0.0000"));
      Trace.WriteLine("   fresh drop: " + (pgd.fFreshDrop / pgd.nGames).ToString("0.000%") + ", Ht: " + (pgd.fFreshDropHt / pgd.nGames).ToString("0.000%"));
      Trace.WriteLine("     injuries: " + (pgd.iInjuries / (double)pgd.iInjuriesPlayer).ToString("0.000%"));
      Trace.WriteLine(" +-------------------------------------------------------+");
      Trace.WriteLine(" |                         GRADES                        |");
      Trace.WriteLine(" +-------------------------------------------------------+");
      Trace.WriteLine(" +------+------+------+------+------+------+------+------+");
      Trace.WriteLine(" |  KP  |  CD  |  SD  |  DM  |  SM  |  OM  |  SF  |  CF  |");
      Trace.WriteLine(" +------+------+------+------+------+------+------+------+");
      Trace.Write(" | ");
      for (byte iGrd = 0; iGrd < pgd.fGrade.Length; iGrd++) {
        if (iGrd == 2 || iGrd == 5 || iGrd == 8) {
          Trace.Write(((pgd.fGrade[iGrd] + pgd.fGrade[iGrd + 1]) / 2f).ToString("0.00") + " | ");
          iGrd++;
        } else {
          Trace.Write(pgd.fGrade[iGrd].ToString("0.00") + " | ");
        }
      }
      Trace.WriteLine("");
      Trace.WriteLine(" +------+------+------+------+------+------+------+------+");
      int iGradeDistTotal = 0;
      foreach (int iGrd in pgd.iGradeDist) iGradeDistTotal += iGrd;
      Trace.WriteLine(" +-------+-------+-------+-------+-------+-------+");
      Trace.WriteLine(" | < 1.5 | < 2.5 | < 3.5 | < 4.5 | < 5.5 | > 5.5 |");
      Trace.WriteLine(" +-------+-------+-------+-------+-------+-------+");
      Trace.Write(" | ");
      for (byte iGrd = 0; iGrd < pgd.iGradeDist.Length; iGrd++) {
        Trace.Write((pgd.iGradeDist[iGrd] / (double)iGradeDistTotal).ToString("00.0%") + " | ");
      }
      Trace.WriteLine("");
      Trace.WriteLine(" +-------+-------+-------+-------+-------+-------+");

      Trace.WriteLine(" +-----------------------------------------------------------------------+");
      Trace.WriteLine(" |                                 SHOOTS                                |");
      Trace.WriteLine(" +-----------------------------------------------------------------------+");
      Trace.WriteLine(" +--------+--------+--------+--------+--------+--------+--------+--------+");
      Trace.WriteLine(" |  < 5m  | < 10m  | < 15m  | < 20m  | < 25m  | < 30m  | < 35m  | > 35m  |");
      Trace.WriteLine(" +--------+--------+--------+--------+--------+--------+--------+--------+");
      string sShootRange = " | ";
      for (int iSht = 0; iSht < pgd.iShootRange.Length; iSht++) {
        sShootRange += (pgd.iShootRange[iSht] / (double)(pgd.iShootsH + pgd.iShootsA)).ToString("00.00%") + " | ";
      }
      Trace.WriteLine(sShootRange);
      Trace.WriteLine(" +--------+--------+--------+--------+--------+--------+--------+--------+");

      string sShootRangeG = " | ";
      for (int iSht = 0; iSht < pgd.iShootRangeGoals.Length; iSht++) {
        sShootRangeG += (pgd.iShootRangeGoals[iSht] / (double)(pgd.iGH + pgd.iGA)).ToString("00.00%") + " | ";
      }
      Trace.WriteLine(sShootRangeG);
      Trace.WriteLine(" +--------+--------+--------+--------+--------+--------+--------+--------+");

      Trace.WriteLine("Total Goals: " + pgd.iGH.ToString() + ":" + pgd.iGA.ToString());
      Trace.WriteLine("Win Home/Draw/Away: " + pgd.iV.ToString() + "/" + pgd.iD.ToString() + "/" + pgd.iL.ToString());
      int iElapsedMin = (int)(iElapsedMilliseconds / 60000.0);
      Trace.WriteLine("Finish date: " + DateTime.Now + ". Elapsed time: " + iElapsedMin.ToString("0m") + ", " + ((iElapsedMilliseconds / 1000.0) - (iElapsedMin * 60)).ToString("00s"));
      Trace.WriteLine("");

      Trace.Unindent();
      Trace.Flush();
      Trace.Listeners.Clear();
    }

    private static void addShootToRange(float fShootDist, ref int[] iShootRange, byte iShootResult, ref int[] iShootRangeGoal)
    {
      int iIx = Math.Min((int)(fShootDist / 5f), iShootRange.Length - 1);
      iShootRange[iIx]++;
      if (iShootResult == 1) iShootRangeGoal[iIx]++;
    }
  }
}
