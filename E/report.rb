ActiveAdmin.register EventReport do
  menu :parent => I18n.t("menu_event"), priority: EventTabPriority::EventReport, label: proc{I18n.t("submenu_event_event_report")}

  index :pagination_total => false do
    column I18n.t("event_report.key"), :eventKey do |eventReport|
      link_to eventReport.eventKey, admin_event_report_path(eventReport.eventKey)
    end
    column I18n.t("event_report.name"), :name
    #column :categoryType do |contents_event| I18n.t("contents_event_category_type.n#{contents_event.categoryType}") end
    column I18n.t("event_report.type"), :eventType do |eventReport|
      I18n.t("event_report.event_type.n#{eventReport.eventType}")
    end
    column I18n.t("event_report.completions"), :completions do |eventReport|
      completions = JSON.parse(eventReport.completions)

      I18n.t("event_report.completions_key") + I18n.t("event_report.split_1") + completions["completionKey"] + I18n.t("event_report.split_0") +
        I18n.t("event_report.reward_completions_key") + I18n.t("event_report.split_1") + completions["rewardCompletionKey"] + I18n.t("event_report.split_0") +
        I18n.t("event_report.contents_shop_id") + I18n.t("event_report.split_1") + completions["contentsShopId"]
    end
    column I18n.t("event_report.startTime"), :startDateTime
    column I18n.t("event_report.endTime"), :endDateTime
    column I18n.t("event_report.updateTime"), :updateDateTime
  end

  show do
    unless event_report.completions.nil?
      completions = JSON.parse(event_report.completions)
      event_report.completion_keys = completions["completionKey"]
      event_report.reward_completion_keys = completions["rewardCompletionKey"]
      event_report.contents_shop_ids = completions["contentsShopId"]
    end
    attributes_table do
      row :eventKey
      row :name
      #row :categoryType do |contents_event| I18n.t("contents_event_category_type.n#{contents_event.categoryType}") end
      row :eventType do |eventReport|
        I18n.t("event_report.event_type.n#{eventReport.eventType}") +"(#{eventReport.eventType})"
      end
      row :mailType do |eventReport|
        I18n.t("mail_main_type.n#{eventReport.mailType}") + "(#{eventReport.mailType})"
      end
      row :completion_keys
      row :reward_completion_keys
      row :contents_shop_ids
      row :startDateTime
      row :endDateTime
      row :openDungeonID
      row :expirationDay
      row :updateDateTime
    end
  end

  form do |f|
    unless event_report.completions.nil?
      completions = JSON.parse(event_report.completions)
      event_report.completion_keys = completions["completionKey"]
      event_report.reward_completion_keys = completions["rewardCompletionKey"]
      event_report.contents_shop_ids = completions["contentsShopId"]
    end

    f.inputs do
      f.input :name, :label => I18n.t("event_report.name")
      #f.input :categoryType, as: :select, include_blank: false, collection: CollectionHelper.contents_event_category_type
      f.input :mailType, :label => I18n.t("event_report.mail_type"), :input_html => { :value => EMailType::EVENT_REPORT_REWARD }#, :input_html => { :disabled => true }
      f.input :completion_keys, :label => I18n.t("event_report.completions_key")
      f.input :reward_completion_keys, :label => I18n.t("event_report.reward_completions_key")
      f.input :contents_shop_ids, :label => I18n.t("event_report.contents_shop_id")
      f.input :startDateTime, :label => I18n.t("event_report.startTime"), as: :datetime_picker
      f.input :endDateTime, :label => I18n.t("event_report.endTime"), as: :datetime_picker
      f.input :openDungeonID, :label => I18n.t("event_report.dungeon_id"), :input_html => { :disabled => true }
      f.input :expirationDay, :label => I18n.t("event_report.expiration_day"), :input_html => { :disabled => true }
      f.input :updateDateTime, :label => I18n.t("event_report.updateTime"), as: :datetime_picker, :input_html => { :disabled => true }
    end
    f.actions
  end

  before_create do |eventReport|
    eventReport.mailType = eventReport.mailType != '' ? eventReport.mailType : 0
    eventReport.updateDateTime = Time.now

    tempArr = Hash.new
    tempArr["completionKey"] = eventReport.completion_keys
    tempArr["rewardCompletionKey"] = eventReport.reward_completion_keys
    tempArr["contentsShopId"] = eventReport.contents_shop_ids

    eventReport.completions = tempArr.to_json
  end

  after_create do |eventReport|
    if eventReport.valid?
      after_changed = eventReport.attributes
      SupportLog.add(commandType: LogCommandType::CREATE, mainType: LogMainType::CONTENTEVENT, adminKey: current_admin_user.id, userKey: 0, value: after_changed)
    end
  end

  before_update do |eventReport|
    eventReport.mailType = eventReport.mailType != '' ? eventReport.mailType : 0
    eventReport.updateDateTime = Time.now

    tempArr = Hash.new
    tempArr["completionKey"] = eventReport.completion_keys
    tempArr["rewardCompletionKey"] = eventReport.reward_completion_keys
    tempArr["contentsShopId"] = eventReport.contents_shop_ids

    eventReport.completions = tempArr.to_json
  end

  after_update do |eventReport|
    if eventReport.valid?
      @old_event_report.attributes = eventReport.attributes
      before_changed, after_changed = ApplicationHelper.get_changed_attributes(@old_event_report)
      SupportLog.add(:commandType=>LogCommandType::UPDATE, :mainType=>LogMainType::CONTENTEVENT, :adminKey=>current_admin_user.id, :userKey=>0, :value=>after_changed, :old_value=>before_changed)
    end
  end

  controller do
    def update
      @old_event_report = EventReport.find(params[:id])
      update!
    end
  end

  filter :eventKey, :label=>I18n.t("event_report.key")
  filter :startDateTime, :label=>I18n.t("event_report.startTime")
  filter :endDateTime, :label=>I18n.t("event_report.endTime")
end
