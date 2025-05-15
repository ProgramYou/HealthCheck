# Health Check Scheduler (HCS)

## Description
The Health Check Scheduler (HCS) determines the active/standby status of servers within a load-balanced cluster. 

A health check endpoint is provided for load balancers and firewalls to poll.  Monitoring this endpoint indicates whether a server should remain active or be switched to standby mode.

Load balancers poll the health check endpoint every 2 to 5 seconds, depending on the firewall type.  To prevent downtime, at least one server should always be active.

## Example
If a server is named servername03, its node index is 3, derived from the trailing number in the name. This trailing number convention is typical for server naming.

The dashboard is available on the /info page. 

## Maintenance Schedule
Using the server's node index, and when the PARITY ACTION is set to 0:
Even-numbered servers will be on standby during even-numbered maintenance periods.
Odd-numbered servers will be offline during odd-numbered maintenance periods.
The Parity Action setting controls the server's active/standby state based on its parity relative to the maintenance schedule.

Valid values for Parity Action:
0 (standby – ready for updates)
1 (active – updates not recommended)

## IIS Site Configuration

Copy the application to the Websites folder and enable its website in IIS.

Add a binding with the fully qualified domain name (FQDN) for the server, e.g., servername01.mydomain.com.
Add a binding for the load-balanced site, e.g., healthcheck.mydomain.com.
Add a host entry for the load-balanced IP address, mapping it to healthcheck.mydomain.com.
Set the health check URL to the endpoint, e.g., http://healthcheck.mydomain.com.
Leave other settings blank unless required for testing.
Settings should be identical across all servers, except when overriding values for testing.

## Info Page

The HCS dashboard is accessible via the /info page.

Each instance of the HCS application displays the status of each server and provides the load balancer endpoint.

## [End]
