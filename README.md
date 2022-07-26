# HRYoobaUnityLibrary.AVPro
## 1.インストール
ProjectSetting/PackageManagerから以下のScopeRegistriesを設定
- Name: `package.openupm.com`
- URL: `https://package.openupm.com`
- Scope: `com.hryooba.library`

PackageManagerからMyRegistriesを選択しパッケージを入れる。

## 2.依存ライブラリ
以下のスコープを追加してください。
```json
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.neuecc.unirx",
        "com.cysharp"
      ]
    }
  ]
```

