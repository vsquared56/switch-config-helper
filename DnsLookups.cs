using Scriban;
using System.Net;
using System.Net.Sockets;
using System;

namespace SwitchConfigHelper
{
    public class DnsLookups : Scriban.Runtime.ScriptObject
    {
        private static int IpCompare(IPAddress x, IPAddress y)
        {


            Byte[] xBytes = x.GetAddressBytes();
            Byte[] yBytes = y.GetAddressBytes();

            if ((x.AddressFamily != y.AddressFamily) || (xBytes.Length != yBytes.Length))
            {
                throw new ArgumentException("Attempting an invalid IP address comparison");
            }

            for (int i = 0; i < xBytes.Length; i++)
            {
                if (xBytes[i] < yBytes[i])
                {
                    return -1;
                }
                else if (xBytes[i] > yBytes[i])
                {
                    return 1;
                }
            }

            return 0;
        }

        //Resolve a hostname and return a single IPv4 address as a string.  Error if 0 or >1 addresses are resolved.
        public static string ResolveA(string hostname)
        {
            IPAddress[] v4Addresses = Array.FindAll(Dns.GetHostAddresses(hostname), a => a.AddressFamily == AddressFamily.InterNetwork);
            if (v4Addresses.Length == 0)
            {
                throw new ArgumentException($"resolve_a: no IP addresses resolved for hostname '{hostname}'");
            }
            else if (v4Addresses.Length > 1)
            {
                throw new ArgumentException($"resolve_a: multiple IP addresses resolved for hostname '{hostname}'");
            }
            else
            {
                Array.Sort(v4Addresses, IpCompare);
                return v4Addresses[0].ToString();
            }
        }

        //Resolve a hostname and return a single IPv4 address as a string.
        //Return only the first address (sorted numerically) if multiple addresses are resolved.
        public static string ResolveSingleA(string hostname)
        {
            IPAddress[] v4Addresses = Array.FindAll(Dns.GetHostAddresses(hostname), a => a.AddressFamily == AddressFamily.InterNetwork);
            if (v4Addresses.Length == 0)
            {
                throw new ArgumentException($"resolve_single_a: no IP addresses resolved for hostname '{hostname}'");
            }
            else
            {
                Array.Sort(v4Addresses, IpCompare);
                return v4Addresses[0].ToString();
            }
        }

        //Resolve a hostname and return one or more IPv4 addresses as an array, sorted numerically.
        public static Array ResolveMultipleA(string hostname)
        {
            IPAddress[] v4Addresses = Array.FindAll(Dns.GetHostAddresses(hostname), a => a.AddressFamily == AddressFamily.InterNetwork);
            if (v4Addresses.Length == 0)
            {
                throw new ArgumentException($"resolve_multiple_a: no IP addresses resolved for hostname '{hostname}'");
            }
            else
            {
                Array.Sort(v4Addresses, IpCompare);
                return v4Addresses;
            }
        }
    }
}