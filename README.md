# AutoDNS
Monitors public IPv4 and IPv6 addresses and automatically updates Cloudflare DNS records on changes.

## Components

- AutoDNS: The command-line interface (CLI) tool.  
  Use this to configure your settings, add/remove DNS records, and manually interact with the Cloudflare API.

- AutoDNSd: The daemon/service that runs in the background.  
  It automatically monitors your public IP address and updates the associated DNS records when a change is detected.

## Installation (.deb)

To install AutoDNS on a Debian-based system (like Ubuntu or Raspberry Pi OS):

1. Download the appropriate `.deb` package for your system architecture:
   - `autodns_x64.deb` for 64-bit PCs
   - `autodns_arm.deb` for 32-bit ARM (e.g., Raspberry Pi)
   - `autodns_arm64.deb` for 64-bit ARM

2. Install the package using `dpkg`:
   ```bash
   sudo dpkg -i autodns.deb
   ```
Once installed, you can run `autodns` from the terminal to get started!
