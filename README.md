# GitSMimeSign

A dotnet global tool to sign commits from the GIT program. Supports GPGSM style output.

It is based off [SMimeSign](https://github.com/github/smimesign) but wrote this program to interop better with the Yubikey.

## How to use

You need a personal SMIME X.509 certificate from a authorised provider.

### Install the global tool

Install using the dotnet global tool utility

```Batchfile
dotnet tool install -g gitsmimesign
```

### Configure git

The following is how to install with GIT versions 2.19 or newer.

#### Configure globally

```Batchfile
git config --global gpg.x509.program gitsmimesign
git config --global gpg.format x509
```

If you want to always use sign commits by default set:

```Batchfile
git config --global commit.gpgsign true
```

#### Configure for local repository only

To configure only a local repository to use the `gitsmimesign`.

```Batchfile
cd \to\path\of\repository
git config --local gpg.x509.program gitsmimesign
git config --local gpg.format x509
```

### Optional: Explictly specify X.509 certificate

If you have multiple X.509 certificates that match your identiy, or would otherwise like to use an alternate X.509 certificate, git can be configured to be aware of this.

Start by listing the available keys:

```batchfile
gitsmimesign --list-keys
```

Identify the desired X.509 certificate from the list, and note the Certificate ID.

#### Configure globally

```batchfile
git config --global user.signingkey CERTIFICATE-ID-HERE
```

#### Configure for local repository only

```batchfile
cd \to\path\of\repository
git config --local user.signingkey CERTIFICATE-ID-HERE
```

### Optional: Set time authority URL

Because `git` does not pass a RFC3161 time stamp authority URL you can set one in the configuration file

Create a file in your user profile directory called `.gitsmimesignconfig`, add the contents modified with your timestamp authority url:

```ini
[Certificate]
TimeAuthorityUrl=http://url.to/timestamp/authority
```

### Optional: Disable telemetry

We track non-personal information to Application Insights, this can be turned off in the case for example your employer disallows telemetry.

In the `.gitsmimesignconfig` file add the following:

```ini
[Telemetry]
Disable=true
```

### Optional: Configure Yubikey

Export out a PFX file from the X.509 certificate. Make a backup in a safe location of this file, if someone gets it they can pretend to be you.

#### Windows

On windows you can use a [Yubikey Mini Smart Driver](https://support.yubico.com/support/solutions/articles/15000006456-yubikey-smart-card-deployment-guide#YubiKey_Minidriver_Installationies8o) but I found the YubiKey manager approach detailed below easier.

I am assuming a pin policy of "once" per session, and no "touch" policy, there are other [options](https://support.yubico.com/support/solutions/articles/15000012643-yubikey-manager-cli-ykman-user-manual#ykman_piv_import-keyk8p1yl). I am also installing into slot 9c which is the signing slot.  

1. Install the [YubiKey manager](https://developers.yubico.com/yubikey-manager-qt/).
1. Open a command line.
1. Run `cd "%PROGRAMFILES%\Yubico\YubiKey Manager"`
1. Change your pin from the default (if you haven't already) and change from the default pin 123456. Run `.\ykman piv change-pin -P 123456 -n <new pin>`
1. Run: `.\ykman piv import-key --pin-policy=default 9c C:\path\to\your.pfx`
1. When prompted, enter the PIN, management key, and password for the PFX.
1. Run: `.\ykman piv import-certificate 9c C:\path\to\your.pfx`
1. When prompted, enter the PIN, management key, and password for the PFX.
1. You may need to logout of your profile if the keys don't show up in SMIMESign below.

#### Mac

1. Install YubiKey Manager
   ```bash
   brew install ykman
   ```
1. Change your pin from the default (if you haven't already) and change from the default pin 123456. Run `ykman piv change-pin -P 123456 -n <new pin>`
1. Run: `ykman piv import-key --pin-policy=default 9c /path/to/your.pfx`
1. When prompted, enter the PIN, management key, and password for the PFX.
1. Run: `ykman piv import-certificate 9c /path/to/your.pfx`
1. When prompted, enter the PIN, management key, and password for the PFX.
1. You may need to logout of your profile if the keys don't show up in SMIMESign below.

#### Linux Ubuntu

1. Install YubiKey manager
   ```bash
   sudo apt-add-repository ppa:yubico/stable
   sudo apt update
   sudo apt install yubikey-manager-qt
   ```
1. Change your pin from the default (if you haven't already) and change from the default pin 123456. Run `ykman piv change-pin -P 123456 -n <new pin>`
1. Run: `ykman piv import-key --pin-policy=default 9c /path/to/your.pfx`
1. When prompted, enter the PIN, management key, and password for the PFX.
1. Run: `ykman piv import-certificate 9c /path/to/your.pfx`
1. When prompted, enter the PIN, management key, and password for the PFX.
1. You may need to logout of your profile if the keys don't show up in SMIMESign below.
