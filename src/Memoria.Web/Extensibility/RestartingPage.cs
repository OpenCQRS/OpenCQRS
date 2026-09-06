namespace Memoria.Web.Extensibility;

/// <summary>
/// The page served by the process that is about to stop.
/// </summary>
/// <remarks>
/// Self-contained rather than a component: it has to survive the shutdown it reports on, so it can
/// depend on nothing the dying application still has to serve. It waits to see the application go
/// down before it starts waiting for it to come back, because the process answers /health right up
/// until it exits.
/// </remarks>
public static class RestartingPage
{
    public const string Html =
        """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Restarting</title>
            <style>
                body {
                    color: #1c2024;
                    font-family: system-ui, -apple-system, "Segoe UI", sans-serif;
                    margin: 0;
                    padding: 3rem 1.5rem;
                }
                h1 { font-size: 1.5rem; font-weight: 600; margin: 0 0 0.5rem; }
                p { color: #5b6570; margin: 0 0 0.5rem; max-width: 34rem; }
                code { font-size: 0.875rem; }
            </style>
        </head>
        <body>
            <h1>Restarting</h1>
            <p>The upload is stored. The application is stopping so it can read the assemblies.</p>
            <p id="status">Waiting for it to come back&hellip;</p>
            <p id="manual" hidden>
                It has not come back. Nothing restarts it on its own &mdash; run it under
                <code>dotnet watch</code>, a container restart policy, IIS or systemd, or start it
                again yourself.
            </p>
            <script>
                let sawItStop = false;
                let attempts = 0;

                async function poll() {
                    attempts += 1;

                    try {
                        const response = await fetch('/health', { cache: 'no-store' });

                        if (response.ok && sawItStop) {
                            location.href = '/settings';
                            return;
                        }

                        if (!response.ok) {
                            sawItStop = true;
                        }
                    } catch {
                        sawItStop = true;
                    }

                    if (attempts > 40) {
                        document.getElementById('status').hidden = true;
                        document.getElementById('manual').hidden = false;
                        return;
                    }

                    setTimeout(poll, 700);
                }

                setTimeout(poll, 700);
            </script>
        </body>
        </html>
        """;
}
