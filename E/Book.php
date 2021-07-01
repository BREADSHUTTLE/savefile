function getSelectAutoScout()
    {
        $scoutInfos = CollectionBookHelper::checkScoutCondition($this->getSessionUserKey());
        // v4.1.0 자동 스카우트
        $selectAutoScouts = CollectionBookRepository::getInstanceOfMySession()->getSelectAutoScouts($this->getSessionUserKey());
        return new Network\Packet(array("scoutInfos" => $scoutInfos, "selectAutoScout" => $selectAutoScouts));
    }

    // v4.1.0
    // 자동 스카우트 설정
    function setSelectAutoScout($scoutID)
    {
        $userKey = $this->getSessionUserKey();

        $scoutData = DataScoutRepository::getDataScoutByID($scoutID);
        $nation = $scoutData->getNation();

        $userNationSelectScoutInfo = CollectionBookRepository::getInstanceOfMySession()->getSelectAutoScoutByNation($userKey, $nation);
        // 해당 국가와 설정할 국가에 동일한 정보가 있을 경우 해제, 아닐경우 삭제 후 추가
        if (null != $userNationSelectScoutInfo) {
            $selectScoutID = $userNationSelectScoutInfo["scoutID"];
            CollectionBookRepository::getInstanceOfMySession()->deleteSelectAutoScoutByNation($userKey, $nation);

            if ($selectScoutID != $scoutID) {
                CollectionBookRepository::getInstanceOfMySession()->addSelectAutoScout($userKey, $nation, $scoutID);
            }
        } else {
            CollectionBookRepository::getInstanceOfMySession()->addSelectAutoScout($userKey, $nation, $scoutID);
        }

        $userSelectAutoScouts = CollectionBookRepository::getInstanceOfMySession()->getSelectAutoScouts($userKey);
        return new Network\Packet(array("selectAutoScout" => $userSelectAutoScouts));
    }

    function autoScoutCharacter($materialCharacterKeys)
    {
        $session = $this->getSession();
        $userKey = $session->getUserKey();

        $lock = new Lock($session->getChannelUserID());
        $gt = new GlobalTransaction([DB::GAME]);
        try {
            // 파라미터 체크
            if ("" == trim($materialCharacterKeys)) {
                throw new GameException(array(ErrorCode::ERROR_SYSTEM_SERVICE_FAILED, "CollectionBookService.autoScoutCharacter", "The parameters do not exist."));
            }

            $characterKeyArr = explode(",", $materialCharacterKeys);
            list($characters, $characterDatas) = CharacterHelper::getCharactersAndData($userKey, $materialCharacterKeys);
            // 전부 가지고 있는 캐릭터들인지
            if (count($characterKeyArr) != count($characters)) {
                throw new GameException(array(ErrorCode::ERROR_GAME_NOT_FOUND_CHARACTERS, "CollectionBookService.autoScoutCharacter", "There is a character that does not exist."));
            }

            $characterArr = [];
            $completionPoint = 0;

            // 국가 전체 스카우트 저장
            $autoScoutArr = array();
            $use_char_data = array();

            // 전체 캐릭터 루프
            foreach ($characters as $character) {
                $characterID = $character->getCharacterID();
                $characterData = DataActorRepository::getDataActorByID($characterID);
                $rootCharacter = $characterData->getRootCharacter();
                $characterNation = $characterData->getTierupNation();
                $characterTier = $characterData->getTier();
                $characterGachaGrade = $characterData->getGachaGrade();

                // 해당 캐릭터가 콜라보인지 체크
                $acquireType = $characterData->getAcquireType();
                if (EAcquireType::SpecialGacha == $acquireType) {
                    throw new GameException(array(ErrorCode::ERROR_GAME_CANNOT_USE_SPECIAL_GACHA_CHARACTER, "CollectionBookService.autoScoutCharacter","Can not use special gacha character."));
                }

                // 해당 캐릭터가 2성인지 체크
                if (ECharTier::Tier4 != $characterTier) {
                    throw new GameException(array(ErrorCode::ERROR_GAME_NOT_FOUND_CHARACTER, "CollectionBookService.autoScoutCharacter", "The character is not a two tier character."));
                }

                // 해당 국가랑 맞는 오토 스카우트 정보
                $selectNationAutoScout = CollectionBookRepository::getInstanceOfMySession()->getSelectAutoScoutByNation($userKey, $characterNation);

                // 해당 캐릭터가 오토 스카우트로 설정된 국가가 아닐 경우 (없을 경우)
                if (null == $selectNationAutoScout) {
                    throw new GameException(array(ErrorCode::ERROR_GAME_INVALID_CHARACTER_NATION, "CollectionBookService.autoScoutCharacter", "Invalid nation."));
                }

                // 오토 스카우트 데이터
                $autoScoutID = $selectNationAutoScout["scoutID"];
                $scoutData = DataScoutRepository::getDataScoutByID($autoScoutID);
                $scoutHeroConditionID = $scoutData->getHeroCondition();

                // 오토로 설정한 스카우트 현재 상태
                $scoutInfo = CollectionBookRepository::getInstanceOfMySession()->getUserScoutCharacter($userKey, $autoScoutID);
                if (null == $scoutInfo) {
                    $scoutInfo["scoutID"] = $autoScoutID;
                    $scoutInfo["saveScore"] = 0;
                    $scoutInfo["heroCondition"] = 0;
                }

                if (!in_array($autoScoutID, array_column($autoScoutArr, "scoutID"))) {
                    // 스카우트 필수 영웅인지
                    if ($scoutHeroConditionID != 0 && $scoutHeroConditionID == $rootCharacter) {
                        $scoutInfo["heroCondition"] += 1;
                    }
                    $scoutInfo["saveScore"] += DataBalance::$SCOUT_T4_SCORE_LEVELS[$characterGachaGrade - 1];
                    array_push($autoScoutArr, $scoutInfo);
                } else {
                    foreach ($autoScoutArr as $autoScout => $value) {
                        if ($autoScoutID == $value["scoutID"]) {
                            // 스카우트 필수 영웅인지
                            if ($scoutHeroConditionID != 0 && $scoutHeroConditionID == $rootCharacter) {
                                $autoScoutArr[$autoScout]["heroCondition"] += 1;
                            }
                            $autoScoutArr[$autoScout]["saveScore"] += DataBalance::$SCOUT_T4_SCORE_LEVELS[$characterGachaGrade - 1];
                        }
                    }
                }

                $completionPoint += DataBalance::$SCOUT_T4_SCORE_LEVELS[$characterGachaGrade - 1];
            }

            $rewardCharacter = array();

            // 스카우트 업데이트
            foreach ($autoScoutArr as $autoScout => $value) {
                $scoutData = DataScoutRepository::getDataScoutByID($value["scoutID"]);
                $before_score = 0;
                $before_condition = 0;

                // 오토로 설정한 스카우트 현재 상태
                $selectScoutInfo = CollectionBookRepository::getInstanceOfMySession()->getUserScoutCharacter($userKey, $value["scoutID"]);
                if (null != $selectScoutInfo) {
                    $before_score = $selectScoutInfo["saveScore"];
                    $before_condition = $selectScoutInfo["heroCondition"];
                }

                // 업데이트
                list($characterArr, $autoScoutArr[$autoScout]["saveScore"], $autoScoutArr[$autoScout]["heroCondition"], $arr_char_data) =
                    CollectionBookHelper::rewardCharacterExosScout($this, $value, $value["scoutID"], $scoutData->getRootCharacter(), $scoutData->getScoutScore(), $scoutData->getHeroCondition());

                foreach ($characterArr as $character) {
                    array_push($rewardCharacter, $character);
                }

                // log
                {
                    foreach ($characterKeyArr as $characterKey) {
                        list($character, $equipItems) = CharacterHelper::getInfoWithCharacter($userKey, $characterKey);
                        // 장착했던 장비 삭제하는 로그
                        $equipItemArr = array();
                        foreach ($equipItems as $equipItem) {
                            if ($characterKey == $equipItem["characterKey"]) {
                                array_push($equipItemArr, $equipItem);
                                MongoDbLogHelper::equip($this, $session, -1, $equipItem, null, "autoScoutCharacter", "autoScoutCharacter");
                            }
                        }
                        // 캐릭터 삭제 로그
                        MongoDbLogHelper::make_use_char_data($use_char_data, $characterKey, $character["characterID"], $character, $equipItems);
                        MongoDbLogHelper::character_delete($this, $session, $characterKey, $character["characterID"], $character, $equipItemArr, null, "autoScoutCharacter");
                    }
                }

                // log
                {
                    $filter = count($arr_char_data) == 0 ? 0 : 1;
                    MongoDbLogHelper::character_scout($this, $use_char_data, $autoScoutArr[$autoScout]["scoutID"], $filter, $before_score, $autoScoutArr[$autoScout]["saveScore"], $before_condition, $autoScoutArr[$autoScout]["heroCondition"], "autoScoutCharacter");
                }

                if($scoutData->getScoutScore() <= $autoScoutArr[$autoScout]["saveScore"] && ($scoutData->getHeroCondition() == 0 || $autoScoutArr[$autoScout]["heroCondition"] > 0)){
                    $autoScoutArr[$autoScout]["state"] = 1;
                } else {
                    $autoScoutArr[$autoScout]["state"] = 0;
                }
            }

            // 캐릭터 삭제
            CharacterRepository::getInstanceOfMySession()->deleteCharacters($userKey, $materialCharacterKeys);
            // 업적 갱신
            CompletionHelper::checkCompletion_ScoutCharacter($this, $completionPoint);

            $gt->commitAllTransactions();
        } catch (Exception $e) {
            $gt->rollbackAllTransactions();
            throw $e;
        } finally {
            $lock->releaseLock();
        }

        return new Network\Packet(array("characters" => $rewardCharacter, "scoutInfo" => $autoScoutArr));
    }