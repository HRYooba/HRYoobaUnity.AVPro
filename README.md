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

### 2.1 映像の表示
Timeline の時刻に合わせたコマは、次の部品で表示する。どちらも Edit mode でも表示する。

- uGUI: RawImage に `MediaPlayerRawImage` を付け、MediaPlayer を割り当てる。縦横比を合わせるときは同じ GameObject に AspectRatioFitter を付ける（映像の縦横比が入る）
- UI Toolkit: `MediaPlayerUIToolkitImage` に UIDocument・要素の name・MediaPlayer を割り当てる。要素が `Image` なら image に、それ以外なら背景画像に出す

上下反転・ガンマ・YCbCr は AVPro の resolve で処理済みのテクスチャを渡す。ピクセルの縦横比（非正方ピクセル）は反映しない。AVPro の `DisplayUGUI`・`ApplyToMaterial` などは MediaPlayer のテクスチャをそのまま出すので、Timeline とは揃わない。

### 2.2 MediaPlayerPlayableTrack の挙動
時刻の主は Timeline。MediaPlayer はそれに従う（MediaPlayer の AutoStart・Loop は不要）。

- `Sync Mode`（トラックの設定）
  - `Buffered`（既定）: 映像を `Lead Frames` コマ先行して再生し、届いたコマを `Buffer Size` 枚貯めて、Timeline の時刻のコマを出す。音が鳴る
    - 音は `Lead Frames` の分だけ絵より先に出る
    - 再生開始・ずれの補正では、今のコマから先行分までをシークで貯め、映像を先行分の位置に置いてから再生する
    - ゲームのフレーム時間が映像の 1 コマより長いと、届かなかったコマの代わりに 2 コマ以内の前のコマが出る。それより古いコマしか無ければ、届くまで直前の表示を保つ
    - 映像が Timeline から 2 コマ（またはゲーム 2 フレーム）以上ずれたら先行し直す。`Time.timeScale` が 1 以外だと、ずれ続けて先行し直しを繰り返す
    - 末尾の `Lead Frames` コマはシークで合わせる（音は鳴らない）。MediaPlayer のループは切る。繰り返しは Timeline で表す
  - `Seek`: 毎コマ Timeline の時刻へシークする。音は鳴らない
- 一時停止中の `PlayableDirector.Evaluate()` は、どちらのモードでもシークで合わせる。Edit mode のスクラブもシークで合わせ、表示は MediaPlayer のテクスチャのまま
- `Wait For Frame`（トラックの設定、既定 ON）: Play mode でシークした後、新しいコマが届くまで待ってから描画へ進む。待つ間メインスレッドが止まる。H.264 などは 1 コマのシークに時間がかかる。AVPro が待ちに対応しない環境では、コマが届いたゲームフレームから出る
- `Auto Rewind`（クリップの設定）: クリップ区間を抜けたとき・区間内で PlayableDirector が止まったときに先頭へ戻す。`PlayableDirector.Pause()` では戻さない
- `Destroy Close`（クリップの設定）: Play mode でグラフが破棄されたとき CloseMedia する
- クリップを作ると長さが映像の長さになる（Timeline ウィンドウで MediaPlayer が開いているとき）。後から合わせるときはクリップの Inspector の `Set Duration`

### 2.3 映像と Timeline の設定
- Timeline の Frame Rate は映像のフレームレート（またはその整数倍）にする。割り切れない値だと、クリップの端が映像のコマ境界に乗らず、他のトラックの切り替わりと映像のコマがずれる
- Windows で HAP の .mov を再生するときは、MediaPlayer の Platform Specific > Windows の Video API を DirectShow にする（Media Foundation では開けない）
