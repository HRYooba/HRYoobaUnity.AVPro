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
時刻の主は Timeline。MediaPlayer はそれに従う（MediaPlayer の AutoStart は不要）。

- `Sync Mode`（トラックの設定）
  - `Buffered`（既定）: 映像を `Lead Frames` コマ先行して再生し、届いたコマを `Buffer Size` 枚貯めて、Timeline の時刻のコマを出す。音が鳴る
    - 音は `Lead Frames` の分だけ絵より先に出る
    - ゲームのフレーム時間が映像の 1 コマより長いと、届かなかったコマの代わりに 1 つ前のコマが出る
    - 映像が Timeline から 2 コマ（またはゲーム 2 フレーム）以上ずれたらシークで戻す。`Time.timeScale` が 1 以外だと、ずれ続けてシークを繰り返す
    - Timeline に合わせたコマは `AlwaysExecuteDisplayUGUI` と `ApplyToRawImage` だけが表示する。AVPro の `ApplyToMaterial` などは MediaPlayer のテクスチャをそのまま出すので揃わない
    - YCbCr など複数テクスチャの映像では使えず、`Seek` と同じ動きになる
  - `Seek`: 毎コマ Timeline の時刻へシークする。音は鳴らない
- 一時停止中の `PlayableDirector.Evaluate()`・Edit mode のスクラブは、どちらのモードでもシークで合わせる
- `Wait For Frame`（トラックの設定、既定 ON）: シークの後、新しいコマが届くまで待ってから描画へ進む。待つ間メインスレッドが止まる。H.264 などは 1 コマのシークに時間がかかる
- `Auto Rewind`（クリップの設定）: クリップ区間を抜けたとき・区間内で PlayableDirector が止まったときに先頭へ戻す。`PlayableDirector.Pause()` では戻さない
- `Destroy Close`（クリップの設定）: Play mode でグラフが破棄されたとき CloseMedia する
- クリップを作ると長さが映像の長さになる（Timeline ウィンドウで MediaPlayer が開いているとき）。後から合わせるときはクリップの Inspector の `Set Duration`

### 2.3 映像と Timeline の設定
- Timeline の Frame Rate は映像のフレームレート（またはその整数倍）にする。割り切れない値だと、クリップの端が映像のコマ境界に乗らず、他のトラックの切り替わりと映像のコマがずれる
- Windows で HAP の .mov を再生するときは、MediaPlayer の Platform Specific > Windows の Video API を DirectShow にする（Media Foundation では開けない）
