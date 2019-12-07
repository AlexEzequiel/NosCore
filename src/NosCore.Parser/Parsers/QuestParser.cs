﻿//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using Serilog;

namespace NosCore.Parser.Parsers

{

    //BEGIN
    //VNUM    {QuestId}     {QuestType}   {autoFinish}    {Daily}	{requiredQuest}    {Secondary}
    //LEVEL   {LevelMin}	{LevelMax}
    //TITLE   {Title}
    //DESC    {Desc}
    //TALK    {StartDialogId}	{EndDialogId}	0	0
    //TARGET  {TargetX}    {TargetY}	{TargetMap}
    //DATA	  0    1	-1     1
    //PRIZE	  {firstPrizeVNUM}	{secondPrizeVNUM}	{thirdPrizeVNUM}	{fourthPrizeVNUM}
    //LINK	  {NextQuest}
    //END
    //
    //#=======

    public class QuestParser
    {
        private readonly string _fileQuestDat = "\\quest.dat";
        private readonly ILogger _logger;
        private readonly IGenericDao<QuestDto> _questDao;
        private readonly IGenericDao<QuestObjectiveDto> _questObjectiveDao;


        public QuestParser(IGenericDao<QuestDto> questDao, IGenericDao<QuestObjectiveDto> questObjectiveDao, ILogger logger)
        {
            _logger = logger;
            _questDao = questDao;
            _questObjectiveDao = questObjectiveDao;
        }

        public void ImportQuests(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object>>
            {
                {nameof(QuestDto.QuestId), chunk => Convert.ToInt16(chunk["VNUM"][0][1])},
                {nameof(QuestDto.QuestType), chunk => Convert.ToInt32(chunk["VNUM"][0][2])},
                {nameof(QuestDto.AutoFinish), chunk => chunk["VNUM"][0][3] == "1"},
                {nameof(QuestDto.IsDaily), chunk => chunk["VNUM"][0][4] == "-1"},
                {nameof(QuestDto.RequiredQuestId), chunk => chunk["VNUM"][0][5] != "-1" ? short.Parse(chunk["VNUM"][0][5]) : (short?)null },
                {nameof(QuestDto.IsSecondary), chunk => chunk["VNUM"][0][6] != "-1"},
                {nameof(QuestDto.LevelMin), chunk => Convert.ToByte(chunk["LEVEL"][0][1])},
                {nameof(QuestDto.LevelMax), chunk => Convert.ToByte(chunk["LEVEL"][0][2])},
                {nameof(QuestDto.Title), chunk => chunk["TITLE"][0][1]},
                {nameof(QuestDto.Desc), chunk => chunk["DESC"][0][1]},
                {nameof(QuestDto.TargetX), chunk =>  chunk["TARGET"][0][1] == "-1" ? (short?)null : Convert.ToInt16(chunk["TARGET"][0][1])},
                {nameof(QuestDto.TargetY), chunk =>  chunk["TARGET"][0][2] == "-1"  ? (short?)null : Convert.ToInt16(chunk["TARGET"][0][2])},
                {nameof(QuestDto.TargetMap), chunk => chunk["TARGET"][0][3] == "-1"  ? (short?)null : Convert.ToInt16(chunk["TARGET"][0][3])},
                {nameof(QuestDto.StartDialogId), chunk => chunk["TARGET"][0][1] == "-1" ? (int?)null :  Convert.ToInt32(chunk["TALK"][0][1])},
                {nameof(QuestDto.EndDialogId), chunk => chunk["TARGET"][0][2] == "-1" ? (int?)null :  Convert.ToInt32(chunk["TALK"][0][2])},
                {nameof(QuestDto.NextQuestId), chunk => chunk["LINK"][0][1] == "-1" ? (short?)null :  Convert.ToInt16(chunk["LINK"][0][1])},
                {nameof(QuestDto.QuestQuestReward), chunk => ImportQuestQuestRewards(chunk)},
                {nameof(QuestDto.QuestObjective), chunk => ImportQuestObjectives(chunk)},
            };
            var genericParser = new GenericParser<QuestDto>(folder + _fileQuestDat, "#=======", 0, actionList, _logger);
            var quests = genericParser.GetDtos("    ");

            _questDao.InsertOrUpdate(quests);
            _questObjectiveDao.InsertOrUpdate(quests.Where(s => s.QuestObjective != null).SelectMany(s => s.QuestObjective));

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.QUESTS_PARSED), quests.Count);
        }

        private List<QuestQuestRewardDto> ImportQuestQuestRewards(Dictionary<string, string[][]> chunk)
        {
            var currentRewards = new List<QuestQuestRewardDto>();
            for (var i = 1; i < 5; i++)
            {
                var prize = Convert.ToInt16(chunk["PRIZE"][0][i]);
                if (prize == 1)
                {
                    continue;
                }

                currentRewards.Add(new QuestQuestRewardDto
                {
                    Id = Guid.NewGuid(),
                    QuestId = Convert.ToInt16(chunk["VNUM"][0][1]),
                    QuestRewardId = prize,
                });
            }

            return currentRewards;
        }

        private List<QuestObjectiveDto> ImportQuestObjectives(Dictionary<string, string[][]> chunk)
        {
            var objectivsDtos = new List<QuestObjectiveDto>();
            foreach (var line in chunk["DATA"])
            {
                if (line[1] != "-1")
                {
                    objectivsDtos.Add(new QuestObjectiveDto
                    {
                        QuestId = Convert.ToInt16(chunk["VNUM"][0][1]),
                        FirstData = Convert.ToInt32(line[1]),
                        SecondData = line[2] == "-1" ? (int?)null : Convert.ToInt32(line[2]),
                        ThirdData = line[3] == "-1" ? (int?)null : Convert.ToInt32(line[3]),
                        FourthData = line[4] == "-1" ? (int?)null : Convert.ToInt32(line[4]),
                        FifthData = line[5] == "-1" ? (int?)null : Convert.ToInt32(line[5]),
                        QuestObjectiveId = Guid.NewGuid()
                    });
                }
            }
            return objectivsDtos;
        }
    }
}