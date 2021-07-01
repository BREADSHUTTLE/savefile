    function getPointEvent()
    {
        $session = $this->getSession();
        $userKey = $session->getUserKey();

        // 상점 리셋
        ShopHelper::resetEventReportShop($userKey);

        // Event Report 정보
        $eventInfo = CommonDBRepository::getContentsEventInfo(EContentsEvent::EventReport);
        if (null == $eventInfo) {
            throw new GameException(array(ErrorCode::ERROR_GAME_NON_EXISTEN_EVENT_REPORT, "CompletionService.getMythEvent"));
        }

        // 포인트 미션 관련 초기화
        CompletionHelper::resetContentsEventCompletionByEventReport($this, $userKey);

        $eventInfo["completions"] = Utils::jsonToArray($eventInfo["completions"]);

        // event mission 에 대한 정보
        $completionMission = CommonDBRepository::getEventCompletions($eventInfo["completions"]["completionKey"]);
        $missionList = array();
        foreach ($completionMission as $mission) {
            $num = $mission["id"];
            unset($mission["id"]);
            $missionList[$num] = $mission;
        }

        // 2021.06.28
        // 출석 안깨지는 이슈 있어서 (정확히는 초기화 관련 부분이 앞단 패킷에 있는데, 그 패킷 시간이 5시가 안넘은채로 처리 된 후, 미션 페이로드가 5시 이후일 경우 문제 발생)
        // 강제로 서버에서 출석 안깨져있으면 깨진걸로 처리한다.
        if (0 < count($missionList)) {
            foreach ($missionList as $key => $value) {
                if (ECompletionType::Attendance == $value["completionType"]) {
                    $completion = UserRepository::getInstanceOfMySession()->getContentsEventCompletionByCompletionKey($userKey, $eventInfo["eventKey"], $key);
                    // 출석 관련 미션이 클리어 되어있지 않을 경우
                    if (null == $completion) {
                        UserRepository::getInstanceOfMySession()->addContentsEventCompletion($userKey, CompletionClearType::CLEAR, $eventInfo["eventKey"], $key, 1);

                        $goodsType = $value["rewardGoodsType"];
                        if (EGoodsType::EventReportPoint == $goodsType) {
                            $goodsValue = $value["rewardGoodsValue"];
                            $maxGoodsValue = UserHelper::getMaxGoods($goodsType, $session->getUserLevel());
                            UserRepository::getInstanceOfMySession()->addUserGoods($session, $userKey, $goodsType, $goodsValue, $maxGoodsValue, PayType::FREE, "getPointEvent");
                        }
                        MongoDbLogHelper::mission_complete($this, $session, ECompletionTermType::Event, $eventInfo["eventKey"], $key, $value["name"]);
                    }
                }
            }
        }

        // Event Report 미션에서 클리어 한 미션
        //$userCompletionList = UserRepository::getInstanceOfMySession()->getContentsEventCompletionByEventKey($userKey, $eventInfo["eventKey"]);
        $userCompletionList = CompletionHelper::getContentsEventCompletionList($userKey, $eventInfo["eventKey"]);

        // event mission 보상 정보도...
        $completionDailyMission = CommonDBRepository::getEventCompletions($eventInfo["completions"]["rewardCompletionKey"]);
        $dailyMissionList = array();
        foreach ($completionDailyMission as $mission) {
            $num = $mission["id"];
            unset($mission["id"]);
            $dailyMissionList[$num] = $mission;
        }

        $eventInfo["completions"]["completionKey"] = explode(",", $eventInfo["completions"]["completionKey"]);
        $eventInfo["completions"]["rewardCompletionKey"] = explode(",", $eventInfo["completions"]["rewardCompletionKey"]);

        // 상점 아이디랑 구매횟수
        $shopList = array();

        $shopIds = explode(",", $eventInfo["completions"]["contentsShopId"]);
        $eventInfo["completions"]["contentsShopId"] = $shopIds;
        // 타입 수정되어야 하는지 물어보기
        $contentsType = ENonBattleType::NB_AirLobby;

        foreach ($shopIds as $id) {
            $contentsShop = ShopRepository::getInstanceOfMySession()->getEventReportShopByContentsTypeAndItemID($userKey, $contentsType, $id);
            if (null != $contentsShop) {
                array_push($shopList, $contentsShop);
            } else {
                $shopInfo = array();
                $shopInfo["userKey"] = $userKey;
                $shopInfo["contentsType"] = $contentsType;
                $shopInfo["contentsShopID"] = $id;
                $shopInfo["buyCount"] = 0;

                array_push($shopList, $shopInfo);
            }
        }

        // 유저 포인트
        $userPoint = UserRepository::getInstanceOfMySession()->getUserGoods($session, $userKey, EGoodsType::EventReportPoint);

        return new Network\Packet(array("eventReportPoint" => $userPoint["eventReportPoint"], "eventList" => $eventInfo,
                                        "missionList" => $missionList, "dailyMissionList" => $dailyMissionList,
                                        "userCompletionList" => $userCompletionList, "contentsShopInfo" => $shopList));
      }