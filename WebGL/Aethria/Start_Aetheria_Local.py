from __future__ import annotations

import functools
import mimetypes
import threading
import webbrowser
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


HOST = "127.0.0.1"
FIRST_PORT = 8765
LAST_PORT = 8795


class AetheriaHandler(SimpleHTTPRequestHandler):
    extensions_map = {
        **SimpleHTTPRequestHandler.extensions_map,
        ".js": "text/javascript; charset=utf-8",
        ".json": "application/json; charset=utf-8",
        ".wasm": "application/wasm",
        ".unityweb": "application/octet-stream",
    }

    def end_headers(self) -> None:
        self.send_header("Cache-Control", "no-store")
        super().end_headers()


def create_server(root: Path) -> tuple[ThreadingHTTPServer, int]:
    handler = functools.partial(AetheriaHandler, directory=str(root))
    for port in range(FIRST_PORT, LAST_PORT + 1):
        try:
            return ThreadingHTTPServer((HOST, port), handler), port
        except OSError:
            continue
    raise RuntimeError(f"No free local port found in {FIRST_PORT}-{LAST_PORT}.")


def main() -> None:
    root = Path(__file__).resolve().parent
    mimetypes.add_type("application/wasm", ".wasm")
    server, port = create_server(root)
    url = f"http://{HOST}:{port}/"

    print()
    print(f"Aetheria is running at {url}")
    print("Keep this window open while playing.")
    print("Press Ctrl+C or close this window to stop the local server.")
    print()

    threading.Timer(0.8, lambda: webbrowser.open(url)).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
