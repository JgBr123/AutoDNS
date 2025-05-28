# AutoDNS
Monitors public IPv4 and IPv6 addresses and automatically updates Cloudflare DNS records on changes.

## Components

- AutoDNS: The command-line interface (CLI) tool.  
  Use this to configure your settings, add/remove DNS records, and manually interact with the Cloudflare API.

- AutoDNSd: The daemon/service that runs in the background.  
  It automatically monitors your public IP address and updates the associated DNS records when a change is detected.
