# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

`JxlDotNet` は [libjxl](https://github.com/libjxl/libjxl)(JPEG XL の C リファレンス実装)を P/Invoke で呼び出す .NET ライブラリ。MVP は単一フレームの RGBA8 decode/encode のみ。`thirdparty/libjxl` は git サブモジュール。

現在の対応 RID は `win-x64` のみ(ネイティブは MSVC + Ninja でビルド)。

## リポジトリ構成

- [src/JxlDotNet/](src/JxlDotNet/) — ライブラリ本体 + NuGet パッケージ化設定
  - [Interop/](src/JxlDotNet/Interop/) — `internal` の 1:1 P/Invoke 層(`NativeMethods` は `LibraryImport` ベース、`net10.0` のソース生成 interop を使用)
  - [JxlDecoder.cs](src/JxlDotNet/JxlDecoder.cs) / [JxlEncoder.cs](src/JxlDotNet/JxlEncoder.cs) — `public` の高レベル API(SafeHandle + IDisposable)
- [tests/JxlDotNet.Tests/](tests/JxlDotNet.Tests/) — xUnit、`net10.0`
- [build/](build/) — ネイティブビルドの MSBuild グルー
  - [LibJxlNative.props](build/LibJxlNative.props) / [LibJxlNative.targets](build/LibJxlNative.targets) — `JxlDotNet.csproj` から import され、ビルド時に CMake を呼ぶ
  - [build-native.ps1](build/build-native.ps1) — vswhere で VS を探し、vcvarsall.bat を展開し、VS 同梱の Ninja + CMake で libjxl を Release shared ビルドする。成果物 DLL 6 本を `src/JxlDotNet/runtimes/win-x64/native/` にコピー
- [artifacts/](artifacts/) — CMake 中間物 + `dotnet pack` の出力(`.gitignore`)
- [thirdparty/libjxl/](thirdparty/libjxl/) — git サブモジュール

## 前提ツール

- .NET SDK 10(`TargetFramework=net10.0`)
- Visual Studio 2022/2026 Community 以上で "C++ によるデスクトップ開発" + "C++ CMake tools for Windows" コンポーネント(MSVC + 同梱 Ninja)
- PowerShell 7(`pwsh`)— MSBuild の `<Exec>` から [build-native.ps1](build/build-native.ps1) を呼ぶために使用
- CMake ≥ 3.16(スタンドアロン CMake でも可、PATH に `cmake` があれば OK)

## 常用コマンド

初回のみサブモジュール取得:

```bash
git submodule update --init --recursive
```

ビルドとテスト(通常の VS Developer 環境は不要。[build-native.ps1](build/build-native.ps1) が内部で vcvarsall を呼ぶ):

```bash
dotnet build JxlDotNet.slnx -c Release
dotnet test  tests/JxlDotNet.Tests/JxlDotNet.Tests.csproj -c Release
dotnet test  tests/JxlDotNet.Tests/JxlDotNet.Tests.csproj -c Release --filter FullyQualifiedName~RoundTripTests
dotnet pack  src/JxlDotNet/JxlDotNet.csproj -c Release -o artifacts/nupkg
```

ネイティブを強制再ビルド(`BuildLibJxl` の Inputs/Outputs 判定で常は skip される):

```bash
rm -rf artifacts/native src/JxlDotNet/runtimes
dotnet build -c Release
```

## アーキテクチャ要点

- **MSBuild → CMake 統合**: [build/LibJxlNative.targets](build/LibJxlNative.targets) が `BeforeTargets=BeforeBuild` で発火。`Inputs=CMakeLists.txt;build-native.ps1`、`Outputs=<stage>/*.dll` で増分判定。実際の差分検知は Ninja に委譲(粗いトリガーで起動し、Ninja が no-op なら数百 ms で抜ける)。
- **ネイティブ成果物は 6 本**: libjxl を `BUILD_SHARED_LIBS=ON` でビルドしているため、`jxl.dll` は `jxl_cms.dll` と brotli(`brotlicommon/brotlidec/brotlienc`)に動的リンクする。`jxl_threads.dll` は独立。これらを全て NuGet の `runtimes/win-x64/native/` に入れる必要がある(依存関係は [build/build-native.ps1](build/build-native.ps1) の `$required` リストを参照)。
- **パラレルランナー**: `JxlResizableParallelRunner` の関数ポインタは `jxl_threads.dll` から `NativeLibrary.GetExport` で取得して decoder/encoder に渡す(managed デリゲートは使わず、GC 回収問題を回避)。
- **構造体の size 整合性**: [Interop/Structs.cs](src/JxlDotNet/Interop/Structs.cs) の `JxlBasicInfo` は末尾に `fixed byte Padding[100]` を持つ。libjxl は前方互換のためこの 100 バイトを要求するので削除しないこと。`JxlColorEncoding` の `double[2]` は `fixed double` で表現しており、x64 で自然アラインメントに頼って C 側とバイナリ互換になっている。
- **MVP の固定条件**: pixel format は RGBA8 interleaved / native endian に固定、色空間は sRGB、エンコード時 `JxlEncoderCloseInput` の後 `JxlEncoderProcessOutput` をバッファ倍々で再呼び出しする単純なループ。アニメーション / progressive / JPEG 再構築 / box メタデータ / 追加チャンネルは非対応。

## よくあるハマり所

- **VS が見つからない/vcvarsall が失敗**: [build-native.ps1](build/build-native.ps1) は `vswhere` 経由で `Microsoft.VisualStudio.Component.VC.Tools.x86.x64` 必須。VS の C++ ワークロードが入っていないとここで落ちる。
- **Ninja が見つからない**: VS Installer で "C++ CMake tools for Windows" にチェックが必要。スタンドアロン Ninja を PATH に置いても可(`Find-BundledNinja` がフォールバック)。
- **CMake のキャッシュ壊れ**: `artifacts/native/win-x64/Release/CMakeCache.txt` を消すと再 configure される。
- **テスト実行時に `DllNotFoundException`**: `ProjectReference` 経由で[build/LibJxlNative.targets](build/LibJxlNative.targets) の `<None Link="%(Filename)%(Extension)" CopyToOutputDirectory=...>` により 6 本の DLL がテスト出力にフラットコピーされる。コピーされていない場合はビルド出力を確認。
