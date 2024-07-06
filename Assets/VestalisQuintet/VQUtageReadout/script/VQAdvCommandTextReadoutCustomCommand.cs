
using System;
using System.IO;
using UnityEngine;
using Utage;

namespace VestalisQuintet.VQUtageReadout
{
    public class VQAdvCommandTextReadoutCustomCommand : AdvCustomCommandManager
    {
        private Microsoft.Extensions.Logging.ILogger logger;

		private GenerateVoiceFiles GenerateVoiceFiles { get; set; } // ボイスファイル生成クラス

		public override void OnBootInit()
		{
			Utage.AdvCommandParser.OnCreateCustomCommandFromID += CreateCustomCommand;
			GenerateVoiceFiles = GameObject.FindObjectOfType<GenerateVoiceFiles>();
			Debug.Assert(GenerateVoiceFiles != null, "GenerateVoiceFilesが見つかりませんでした。");
		}

		void OnDestroy()
		{
			Utage.AdvCommandParser.OnCreateCustomCommandFromID -= CreateCustomCommand;
		}

		void Start()
		{
            logger = new UnityLogger();
		}

		//AdvEnginのクリア処理のときに呼ばれる
		public override void OnClear()
		{
		}
 		
		//カスタムコマンドの作成用コールバック
		public void CreateCustomCommand(string id, StringGridRow row, AdvSettingDataManager dataManager, ref AdvCommand command )
		{
			switch (id)
			{
                // 読み上げコマンドを登録
				case AdvCommandParser.IdText:
					Debug.Log("TextCommandをオーバーライドしました。");
					command = new VQAdvCommandTextReadout(row, dataManager, GenerateVoiceFiles);
					break;
			}
		}
    }

    public class VQAdvCommandTextReadout : AdvCommandText
    {
		public GenerateVoiceFiles GenerateVoiceFiles { get; set; } // ボイスファイル生成クラス

		public VQAdvCommandTextReadout(StringGridRow row, AdvSettingDataManager dataManager, GenerateVoiceFiles generateVoiceFiles)
			: base(row, dataManager)
		{
			GenerateVoiceFiles = generateVoiceFiles;

			//ボイスファイル設定
			InitVoiceFile(dataManager);

			//ページコントロール
			this.PageCtrlType = ParseCellOptional<AdvPageControllerType>(AdvColumnName.PageCtrl, AdvPageControllerType.InputBrPage);
			this.IsNextBr = AdvPageController.IsBrType(PageCtrlType);
			this.IsPageEnd = AdvPageController.IsPageEndType(PageCtrlType);

			//エディター用のチェック
			if (AdvCommand.IsEditorErrorCheck)
			{
				TextData textData = new TextData(ParseCellLocalizedText());
				if (!string.IsNullOrEmpty(textData.ErrorMsg))
				{
					Debug.LogError(ToErrorString(textData.ErrorMsg));
				}
			}
		}

		//ページ用のデータからコマンドに必要な情報を初期化
		public override void InitFromPageData(AdvScenarioPageData pageData)
		{
			this.PageData = pageData;
			this.IndexPageData = PageData.TextDataList.Count;
			PageData.AddTextData(this);
			PageData.InitMessageWindowName(this, ParseCellOptional<string>(AdvColumnName.WindowType, ""));
		}

		//エンティティコマンドとして利用
		public new void InitOnCreateEntity(AdvCommand original)
		{
			VQAdvCommandTextReadout originalText = original as VQAdvCommandTextReadout; 
			this.PageData = originalText.PageData;
			PageData.ChangeTextDataOnCreateEntity(originalText.IndexPageData, this);
		}

		//コマンド実行
		public override void DoCommand(AdvEngine engine)
		{
			Debug.Log("VQAdvCommandTextReadoutCustomCommandのDoCommand実行");
			if (IsEmptyCell(AdvColumnName.Arg1))
			{
				engine.Page.CharacterInfo = null;
			}
			if (null != VoiceFile)
			{
				if (!engine.Page.CheckSkip () || !engine.Config.SkipVoiceAndSe) 
				{
					//キャラクターラベル
					engine.SoundManager.PlayVoice ( engine.Page.CharacterLabel, VoiceFile);
					engine.ScenarioSound.SetVoiceInScenario(engine.Page.CharacterLabel, VoiceFile);
				}
			}
			engine.Page.UpdatePageTextData(this);
		}

		//コマンド終了待ち
		public override bool Wait(AdvEngine engine)
		{
			return engine.Page.IsWaitTextCommand;
		}

		public override void OnChangeLanguage(AdvEngine engine)
		{
			Debug.Log("VQAdvCommandTextReadoutCustomCommandのOnChangeLanguage実行");
			if (!LanguageManagerBase.Instance.IgnoreLocalizeVoice)
			{
				//ボイスファイル設定
				InitVoiceFile(engine.DataManager.SettingDataManager);
			}
		}

		protected override void InitVoiceFile(AdvSettingDataManager dataManager)
		{
			//ボイスファイル設定
			// UtageSettings.Instance.overridePartVoiceByReadOutが有効である場合、SetGeneratedVoice()を優先する
			string voice = ParseCellOptional<string>(AdvColumnName.Voice, "");
            if (string.IsNullOrEmpty(voice) || (UtageSettings.Instance.useReadOut && UtageSettings.Instance.overridePartVoiceByReadOut))
            {
				if(GenerateVoiceFiles?.generateFinished == true)
				{
					// 話者とテキストのハッシュを元にボイスファイルを取得
					SetGeneratedVoice();
				}
            }
            else
            {
                VoiceFile = ParseVoiceSub(dataManager, voice);
            }
        }

		/// <summary>
		/// 生成したボイスファイルを設定する
		/// </summary>
        public void SetGeneratedVoice()
        {
            // 話者とテキストのハッシュを元にボイスファイルを取得
			// CharacterOverride列に話者名が設定されている場合は、その話者名を取得する
			string overrideTalkChar = TryParseCell<String>("CharacterOverride", out var tmpOverrideChar) ? tmpOverrideChar : null;
            string talkChar; // 話者
            if (!TryParseCell<string>("Arg1", out talkChar))
            {
                talkChar = GenerateVoiceFiles.DESCRIPTIVE;
            }
			if(overrideTalkChar != null && overrideTalkChar != "")
			{
				talkChar = overrideTalkChar;
			}

            string message = ParseCellLocalizedText();

            // 話者の情報とテキスト全文を組み合わせる
            string combinedText = talkChar + "_" + message;

			Debug.Log($"アセットマネージャへの音声登録処理開始:{combinedText}");

            // 組み合わせた文字列のハッシュ値を生成する
            string hash = ComputeHash.ComputeSha256Hash(combinedText);

            string readoutDir = GenerateVoiceFiles.getOutputDir();

            string readoutFileName = Path.Combine(readoutDir, hash + ".wav").Replace("\\", "/");

            // ファイルが存在する時のみAddLoadFileを呼ぶ
            if (File.Exists(readoutFileName))
            {
                Debug.Log($"ボイスファイル{readoutFileName}が存在するため、ロードします。");
                VoiceFile = AssetFileManager.GetFileCreateIfMissing(readoutFileName, new AdvVoiceSetting(this.RowData));
            }
        }

        public override void UpdatePageCtrlType()
		{
			var textData = new TextData(ParseCellLocalizedText());

			//テキストスキップタグで、ページコントロールを無視する場合
			var parsedText = textData.ParsedText;
			if (parsedText.SkipText &&  !parsedText.EnablePageCtrlOnSkipText)
			{
				if (this.PageCtrlType == AdvPageControllerType.InputBrPage)
				{
					this.PageCtrlType = AdvPageControllerType.BrPage;
				}
				else
				{
					this.PageCtrlType = AdvPageControllerType.Next;
					
				}
			}
			this.IsNextBr = AdvPageController.IsBrType(PageCtrlType);
		}

		//ページ区切り系のコマンドか
		public override bool IsTypePage() { return true; }
		//ページ終端のコマンドか
		public override bool IsTypePageEnd() { return IsPageEnd; }
		public new bool IsPageEnd { get; private set; }
		public new bool IsNextBr { get; private set; }
		public new AdvPageControllerType PageCtrlType { get; private set; }

		public new AssetFile VoiceFile { get; private set; }

		AdvScenarioPageData PageData { get; set; }
		int IndexPageData { get; set; }
    }

}

