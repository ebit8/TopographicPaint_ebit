# 手描き等高線立体化アプリ

マウスで描いたオリジナルの等高線図を、リアルタイムで立体化するアプリケーションです。

# DEMO
下の画像をクリックするとYoutubeに飛びます
[!['デモ動画'](https://github.com/ebit8/TopographicPaint_git/assets/112364174/ab30e9d9-4ed8-4a62-99b0-f043ff5e174c)](https://youtu.be/T0U8SXIowq4)

# Features

* 描いた等高線図がすぐに立体化される
* 消しゴム機能を使うことで、等高線図の修正が簡単にできる
* 立体化された地形は、マウス操作でグリグリ見ることができる
* 標高によってグラデーションがかかっているので高さがわかりやすい

# Requirement 

* Unity 2019.4.36f1
* OpenCVforUnity 2.5.7 (https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088?locale=ja-JP)

※デモアプリ自体はWindows10または11で動かせます。ただし、CPUの性能がある程度ないと快適に動かないかもしれません（Ryzen 7 2700では快適に動くことを確認）。

# Installation

デモアプリの実行：このリポジトリをzipでダウンロードして展開、TPBuildフォルダ内のTopographicPaint.exeを実行

ビルド方法：

# Usage

* 画面右側の白い領域内で、マウスの右ボタンをドラッグして円を描くと、その変更が画面左側の3Dモデルに反映されます。
  * 右上のペンボタンを押すと線を描くことが、消しゴムボタンを押すと線を消すことができるようになります。
* マウスカーソルが画面左側にあるときに、視点を変更することができます。
  * 視点の回転：右ボタンをドラッグ
  * 視点の移動：ホイールドラッグ
  * ズームイン/アウト：ホイールをスクロール
* クリアボタンをクリックすることで、描いた内容をリセットできます。

![usage_2](https://github.com/ebit8/TopographicPaint_git/assets/112364174/537e35aa-98ca-4cd9-8bed-ef2a93714381)

# Author

* 岡﨑亮満
* 関西学院大学大学院理工学研究科人間システム工学専攻山本研究室
* ryoma60neur@gmail.com

# License
GNU General Public License v3.0
