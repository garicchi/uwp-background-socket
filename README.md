# What is this
UWPでバックグラウンドでソケット待ち受けをするサンプルです。SocketActivityTriggerというものを使えばUWPのバックグラウンドタスクでSocketの受信に反応することができます。

SocketActivityTriggerの使い方としては大きく2種類あり

1. フォアグランドでコネクトしておいたSocketをバックグラウンドに引き継ぐ
2. フォアグランドでリッスンしているリスナーをバックグラウンドに引き継ぐ

このサンプルは後者になります。
前者のサンプルは[かずきさんのブログ](https://www.google.co.jp/url?sa=t&rct=j&q=&esrc=s&source=web&cd=1&cad=rja&uact=8&ved=0ahUKEwjY2ZzYoZzSAhWME7wKHbL9DGEQFggcMAA&url=http%3A%2F%2Fblog.okazuki.jp%2Fentry%2F2016%2F04%2F08%2F044312&usg=AFQjCNEMq5FwTZFkzScDdBNwLdbGlebaaQ&sig2=XLlYflhw12_YwSFajECXRQ)を参考にしてください。

このサンプルではフォアグランドでリッスンしたリスナーをバックグラウンドで引き継ぎ、バックグラウンドタスクの状態でもソケットを待ち受けることができます。さらにソケットが閉じられても再度リッスンします。OSが再起動しても再度リッスンをバックグラウンドでします。

# Manifest
マニフェストで許可しなければいけないものは

- 機能
    - インターネット(クライアント)
    - インターネット(クライアントとサーバー)
    - プライベートネットワーク(クライアントとサーバー)
- 宣言
    - バックグラウンドタスク(デバイス使用トリガーにチェックとシステムイベントにチェック、エントリポイントをバックグラウンドタスクのプロジェクトに)

# How to use
cloneしてBackgroundSocketSampleプロジェクトをデバッグ開始します。Register Background Taskボタンをクリックすると表示されているIPとポート番号でリッスン開始します。

あとはアプリを閉じてもらって、テキトーなソケットを投げるアプリ(AndroidならSimpleSocketTester)からソケットを投げるとバックグラウンドにいても反応します。

動作の様子は[こちら](https://youtu.be/assZGebCOuY)