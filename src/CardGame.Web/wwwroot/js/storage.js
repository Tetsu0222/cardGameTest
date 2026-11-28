// ブラウザの保存領域(localStorage)の読み書き。
// プライベートブラウズやサイトデータの制限で使えない場合があるため、失敗しても例外にせず null / false を返す。

export function getItem(key) {
    try {
        return window.localStorage.getItem(key);
    } catch {
        return null;
    }
}

export function setItem(key, value) {
    try {
        window.localStorage.setItem(key, value);
        return true;
    } catch {
        return false;
    }
}
