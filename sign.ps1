nuget sign Build\Release\Semiodesk.Trinity.*.nupkg -Timestamper http://sha256timestamp.ws.symantec.com/sha256/timestamp  -CertificateFingerprint 0b5f624d32cb7e5549405b32a8a0a9aebfd1859a

nuget sign Build\Release\stores\virtuoso\Semiodesk.Trinity.Virtuoso.*.nupkg -Timestamper http://sha256timestamp.ws.symantec.com/sha256/timestamp  -CertificateFingerprint 0b5f624d32cb7e5549405b32a8a0a9aebfd1859a