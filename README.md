# HRYoobaUnity.AVPro
## 1 インストール
### 1.1 パッケージマネージャーの設定
ProjectSetting/PackageManagerから以下のScopeRegistriesを設定
- Name: `package.openupm.com`
- URL: `https://package.openupm.com`
- Scope(s): 
  - `com.hryooba`
  - `com.cysharp`
  - `org.nuget`

### 1.2 インストール
PackageManagerからMyRegistriesを選択しパッケージを入れる。

## 2 Timeline Support
<img src="https://github.com/user-attachments/assets/d680f978-7af3-4b58-99a2-793612b8e5cf">  

### 2.1 AlwaysExecuteDisplayUGUIを生成
<img src="https://github.com/user-attachments/assets/89bddac3-affe-4d5e-b29e-b4f7bcc8990d">  

### 2.2 MediaPlayerPlayableTrack の挙動
- クリップのローカル時刻（Clip In・Speed を含む）から映像のコマ番号を求め、コマが変わったときだけシークする
- MediaPlayer の再生は止める。時刻は Timeline が決める（MediaPlayer の AutoStart は不要）
- `Wait For Frame`（トラックの設定、既定 ON）: シーク後に新しいコマが届くまで待ってから描画へ進む。映像と ActivationTrack などが同じゲームフレームで切り替わる
  - 待つ間メインスレッドが止まる。全コマがキーフレームの映像（HAP・ProRes など）向け。H.264 などでは OFF にする（OFF だと映像は 1 ゲームフレーム遅れて出る）
  - AVPro が WaitForNextFrame に対応しないプラットフォームではこの待ちが効かず、OFF と同じになる
- `Auto Rewind`（クリップの設定）: クリップ区間を抜けたとき・区間内で PlayableDirector が止まったときに先頭へ戻す。`PlayableDirector.Pause()` では戻さない
- `Destroy Close`（クリップの設定）: Play mode でグラフが破棄されたとき CloseMedia する
- クリップを作ると長さが映像の長さになる（Timeline ウィンドウで MediaPlayer が開いているとき）。後から合わせるときはクリップの Inspector の `Set Duration`

### 2.3 映像と Timeline の設定
- Timeline の Frame Rate は映像のフレームレート（またはその整数倍）にする。割り切れない値だと、クリップの端が映像のコマ境界に乗らず、他のトラックの切り替わりと映像のコマがずれる
- Windows で HAP の .mov を再生するときは、MediaPlayer の Platform Specific > Windows の Video API を DirectShow にする（Media Foundation では開けない）
