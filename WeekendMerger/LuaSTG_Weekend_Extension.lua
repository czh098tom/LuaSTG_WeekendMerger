local IsLuaSTGSub = type(lstg.GetVersionNumber) == "function"
local IsLuaSTGPlus = not (IsLuaSTGSub)

local print = lstg and lstg.Print or Print or print
local concat = table.concat
local insert = table.insert
local traceback = debug.traceback
local select = select
local pairs = pairs
local ipairs = ipairs
local tostring = tostring

---获取当前代码层级结构
---@return string
local function GetTraceBack()
    return traceback("", 2)
end

---输出日志
local function Log(...)
    local list = { ... }
    for i = 1, select("#", ...) do
        list[i] = tostring(list[i])
    end
    print("[LuaSTG Weekend Extension] " .. concat(list, "\t"))
end

---当前是否已经在处理用户逻辑
---@type boolean
local Processing = false

---当前正在处理的用户名称
---@type string
local CurrentAuthor

---记录的用户顺序
local RecordAuthorList = {}
---当前记录的用户序号
local RecordAuthorIndex = 0

---已记录的编辑器定义列表
local RecordEditorDefinedClass = {}
local _original_editor_class = _editor_class
local _original_editor_tasks = _editor_tasks
local _original_sc_table = _sc_table

---判断一个类是否是boss
---@return boolean
local function IsBoss(class)
    if class == boss then
        return true
    elseif class.base
            and class.base ~= object
            and class.base ~= background
            and class.base ~= bullet
            and class.base ~= laser
            and class.base ~= laser_bent
            and class.base ~= item
            and class.base ~= player
    then
        return IsBoss(class.base)
    end
    return false
end

---枚举boss
---@param classes table
---@return table
local function EnumBosses(classes)
    local result = {}
    for k, v in pairs(classes) do
        if IsBoss(v) then
            v._class_name = k
            insert(result, v)
        end
    end
    return result
end

---开始记录编辑器定义类
---@param name string
local function StartRecordingEditorClass(name)
    if not RecordEditorDefinedClass[name] then
        RecordEditorDefinedClass[name] = {
            author = name,
            res = {},
            class = {},
            tasks = {},
            _sc_table = {},
            _sorted_sc_table = {}
        }
    end
    local record = RecordEditorDefinedClass[name]
    _editor_class = record.class
    _editor_tasks = record.tasks
    _sc_table = record._sc_table
end

---结束记录编辑器定义类
---@param name string
local function EditorClassRecordFinish(name)
    local class, tasks, sc = _editor_class, _editor_tasks, _sc_table
    local bosses = EnumBosses(class)
    local _sorted_sc_table = RecordEditorDefinedClass[name]._sorted_sc_table
    local base_sc_list_sorted = {}
    for _, card in ipairs(sc) do
        local c1 = class[card[1]]   -- boss
        local c2 = card[2]          -- 卡名
        local c3 = card[3]          -- 卡
        local c4 = card[4]          -- 编号
        local c5 = card[5]          -- 是否perform
        base_sc_list_sorted[c1] = base_sc_list_sorted[c1] or {}
        insert(base_sc_list_sorted[c1], { c1, c2, c3, c4, c5 })
    end
    for _, boss in ipairs(bosses) do
        local data = {
            name = boss.name,
            card = {},
        }
        if boss.cards then
            local index = 0
            for i, card in ipairs(boss.cards) do
                if card.is_combat then
                    local found_index
                    for bi, v in ipairs(base_sc_list_sorted[boss] or {}) do
                        if v[4] == i then
                            found_index = bi
                            break
                        end
                    end
                    local scp
                    if found_index then
                        scp = {
                            boss._class_name,
                            boss.name,
                            card.name,
                            card,
                            i,
                            card.perform or base_sc_list_sorted[boss][found_index][5]
                        }
                    else
                        index = index + 1
                        scp = {
                            boss._class_name,
                            boss.name,
                            ("Non-Spellcard #%d - [auto added by extension]"):format(index),
                            card,
                            i,
                            false
                        }
                    end
                    insert(data.card, scp)
                    Log(("Register card to boss %q : %q"):format(boss.name, scp[3]))
                end
            end
        end
        insert(_sorted_sc_table, data)
    end
    _editor_class = _original_editor_class
    _editor_tasks = _original_editor_tasks
    _sc_table = _original_sc_table
    for k, v in pairs(class) do
        if _editor_class[k] then
            Log(("发现 %q 的工程内编辑器定义与其他人的定义存在重名 class : %q"):format(name, k))
        end
        _editor_class[k] = v
    end
    for k, v in pairs(tasks) do
        if _editor_tasks[k] then
            Log(("发现 %q 的工程内编辑器定义与其他人的定义存在重名 task : %q"):format(name, k))
        end
        _editor_tasks[k] = v
    end
    for k, v in ipairs(sc) do
        insert(_sc_table, v)
    end
end

---执行加载子工程前的处理逻辑
---@param name string
function BeforeLoadSubProject(name)
    if Processing then
        Log(("在未声明结束当前正在处理的工程的情况下开始了对 %q 的工程的处理"):format(name))
    end
    CurrentAuthor = name
    RecordAuthorIndex = RecordAuthorIndex + 1
    RecordAuthorList[name] = RecordAuthorIndex
    RecordAuthorList[RecordAuthorIndex] = name
    Processing = true
    StartRecordingEditorClass(name)
    Log(("开始处理 %q 的工程"):format(name))
end

---执行加载子工程后的处理逻辑
---@param name string
function AfterLoadSubProject(name)
    if not (Processing) then
        Log(("在未声明进行处理的情况下结束了对 %q 的工程的处理"):format(name))
    end
    EditorClassRecordFinish(name)
    Log(("%q 的工程处理完毕"):format(name))
    CurrentAuthor = nil
    Processing = false
end

local NonString = "---"
local renderTTF = function(text, x, y, a, r, g, b)
    RenderTTF("sc_pr", text, x, x, y, y, Color(a, r, g, b), "centerpoint")
end
local renderTTFL = function(text, x, y, a, r, g, b)
    RenderTTF("sc_pr", text, x, x, y, y, Color(a, r, g, b), "vcenter", "left")
end
local renderTTFR = function(text, x, y, a, r, g, b)
    RenderTTF("sc_pr", text, x, x, y, y, Color(a, r, g, b), "vcenter", "right")
end

local last_index1, last_index2
local menu = Class(object)
sc_pr_menu = menu
function menu:init(exit_func)
    self.x = screen.width * 0.5 + screen.width
    self.y = screen.height * 0.5
    self.bound = false
    self.locked = true
    self.alpha = 1
    self.exit_func = exit_func
    self.index1 = 1
    self.index2 = 1
    self.authors = RecordAuthorList
    self.cards = {}
    self.texts = {}
    self.draws = {}
    if lstg.var.sc_pr and last_index1 and last_index2 then
        self.index1, self.index2 = last_index1, last_index2
        last_index1, last_index2 = nil, nil
        lstg.var.sc_pr = nil
    end
    menu.UpdateList(self)
    menu.UpdateDraws(self)
end
function menu:frame()
    task.Do(self)
    if self.locked then
        return
    end
    menu.UpdateInput(self)
end
function menu:render()
    SetViewMode("ui")
    SetImageState("white", "", Color(0xC0000000))
    do
        RenderRect("white", self.x - 200, self.x + 200, self.y + 150, self.y + 200)
        for i = -2, 2 do
            local r = sin(i / 2) * 90
            local ar = abs(r)
            local name = self.authors[(self.index1 + i - 1) % #self.authors + 1]
            local alpha = 255 - 192 * ar
            local x = self.x + 90 * r
            local y = self.y + 165 + 15 * ar
            renderTTF(name, x, y, alpha, 255, 255, 255)
        end
    end
    do
        RenderRect("white", self.x - 200, self.x + 200, self.y - 200, self.y + 100)
        menu.RenderSpellcardList(self, self.draws)
    end
end
function menu:RenderSpellcardList(texts)
    for i = 1, 7 do
        local data = texts[i]
        if data then
            if data[2] then
                local gb = data[4] == self.index2 and 0 or 255, 255
                renderTTFR(data[5], self.x + 180, self.y + 85 - 45 * (i - 1), 255, 255, gb, gb)
            else
                renderTTFL(data[3], self.x - 180, self.y + 85 - 45 * (i - 1), 255, 255, 255, 255)
            end
        else
            renderTTF(NonString, self.x, self.y + 85 - 45 * (i - 1), 255, 255, 255, 255)
        end
    end
end
function menu:UpdateInput()
    local lastKey = GetLastKey()
    local flag1, flag2 = false, false
    if lastKey == setting.keys.left then
        self.index1 = self.index1 - 1
        flag1 = true
    end
    if lastKey == setting.keys.right then
        self.index1 = self.index1 + 1
        flag1 = true
    end
    self.index1 = (self.index1 - 1) % #self.authors + 1
    if lastKey == setting.keys.up then
        self.index2 = self.index2 - 1
        flag2 = true
    end
    if lastKey == setting.keys.down then
        self.index2 = self.index2 + 1
        flag2 = true
    end
    self.index2 = (self.index2 - 1) % #self.cards + 1
    if flag1 or flag2 then
        PlaySound("select00", 0.3)
        if flag1 then
            menu.UpdateList(self)
            self.index2 = 1
        end
        menu.UpdateDraws(self)
    end
    if KeyIsPressed "shoot" then
        local data = self.cards[self.index2]
        if data then
            PlaySound("ok00", 0.3)
            last_index1, last_index2 = self.index1, self.index2
            menu.SetCardData(self, data)
            if self.exit_func then
                self.exit_func(1)
            end
        else
            PlaySound("invalid", 0.5)
        end
    elseif KeyIsPressed "spell" then
        PlaySound("cancel00", 0.3)
        menu.SetCardData(self, nil)
        if self.exit_func then
            self.exit_func(nil)
        end
    end
end
function menu:UpdateList()
    local cards = {}
    self.cards = cards
    local _sorted_sc_table = self.authors[self.index1] and RecordEditorDefinedClass[self.authors[self.index1]]._sorted_sc_table
    for _, data in ipairs(_sorted_sc_table or {}) do
        for _, card in ipairs(data.card) do
            insert(cards, { data.name, card[2], card[3], card[1], card[5], card[6] })
        end
    end
    local n = 0
    local texts = {}
    self.texts = texts
    do
        local boss_name
        local index = 0
        for _, card in ipairs(self.cards) do
            n = n + 1
            if boss_name ~= card[2] then
                boss_name = card[2]
                insert(texts, { n, false, boss_name })
                n = n + 1
            end
            index = index + 1
            insert(texts, { n, true, boss_name, index, card[3] })
        end
    end
end
function menu:UpdateDraws()
    local draws = {}
    self.draws = draws
    local texts = self.texts
    local n = #texts
    if n <= 7 then
        for i = 1, n do
            insert(draws, texts[i])
        end
    else
        local lineIndex
        for i = 1, n do
            if texts[i][2] and texts[i][4] == self.index2 then
                lineIndex = i
            end
        end
        if lineIndex <= 4 then
            for i = 1, 7 do
                insert(draws, texts[i])
            end
        else
            local remain = #texts - lineIndex
            if remain > 3 then
                for i = -3, 3 do
                    insert(draws, texts[lineIndex + i])
                end
            else
                for i = -6, 0 do
                    insert(draws, texts[#texts + i])
                end
            end
            if draws[1][2] and not (draws[2][2]) then
                draws[1] = { draws[1][1], false, draws[1][3] }
            elseif draws[1][2] and draws[2][2] then
                draws[1] = { draws[1][1], false, draws[2][3] }
            end
        end
    end
end
function menu:SetCardData(data)
    if data then
        lstg.var.sc_pr = {
            class_name = data[4],
            index = data[5],
            perform = data[6],
        }
    else
        lstg.var.sc_pr = nil
    end
end

stage.group.DefStageFunc("Spell Practice@Spell Practice", "init", function(self)
    _init_item(self)
    New(mask_fader, "open")
    New(_G[lstg.var.player_name])
    task.New(self, function()
        if lstg.var.sc_pr then
            local boss_class = _editor_class[lstg.var.sc_pr.class_name]
            local card_index = lstg.var.sc_pr.index
            local cards = { boss_class.cards[card_index] }
            if lstg.var.sc_pr.perform then
                if boss_class.cards[card_index - 1] then
                    insert(cards, 1, boss_class.cards[card_index - 1])
                end
            else
                insert(cards, 1, boss.move.New(0, 144, 60, MOVE_DECEL))
            end
            local boss_bgm = boss_class.bgm
            if boss_bgm and boss_bgm:match("%S") then
                LoadMusicRecord(boss_bgm)
            else
                boss_bgm = "spellcard"
                LoadMusic("spellcard", music_list.spellcard[1], music_list.spellcard[2], music_list.spellcard[3])
            end
            New(boss_class._bg or temple_background)
            task._Wait(30)
            local _, bgm = EnumRes("bgm")
            for _, v in pairs(bgm) do
                local state = GetMusicState(v)
                if v == boss_bgm then
                    if state == "paused" then
                        ResumeMusic(v)
                    elseif state == "stopped" then
                        PlayMusic(v)
                    end
                else
                    StopMusic(v)
                end
            end
            local _ref = New(boss_class, cards)
            last = _ref
            while IsValid(_ref) do
                task.Wait()
            end
            task._Wait(150)
        end
        if ext.replay.IsReplay() then
            ext.pop_pause_menu = true
            ext.rep_over = true
            lstg.tmpvar.pause_menu_text = { "Replay Again", "Return to Title" }
        else
            ext.pop_pause_menu = true
            lstg.tmpvar.death = false
            lstg.tmpvar.pause_menu_text = { "Continue", "Quit and Save Replay", "Return to Title" }
        end
        task._Wait(60)
    end)
    task.New(self, function()
        while coroutine.status(self.task[1]) ~= "dead" do
            task.Wait()
        end
        New(mask_fader, "close")
        _stop_music()
        task.Wait(30)
        stage.group.FinishStage()
    end)
end)

Log("Inited in " .. (IsLuaSTGSub and ("LuaSTG Sub " .. lstg.GetVersionNumber()) or "LuaSTG Plus"))