# Installation procedure



## Installing the components

The easiest way to install/run a Keycloak container for development purpose is suggested from the Keycloak website:

```
docker run -p 8080:8080 -e KEYCLOAK_ADMIN=admin -e KEYCLOAK_ADMIN_PASSWORD=admin quay.io/keycloak/keycloak:19.0.1 start-dev
```

This setup has several issues, mostly because of the `start-dev` command.

- Keycloak persist the data inside the Windows folder used by WSL2 to store the container state. Later in this document we will see how to overcome this by using `-v` and saving the state in a Windows folder that is shared with the container
- There is no way to "restart" the container by changing the environment variables. This means that if you decide to add/remove a Keycloak feature at the following runs, you have to start from scratch.
- Keycloak only listen to `http` instead of `https`. Although this looks a good choice from the development perspective, this is an horrible idea because browsers like Chrome and many security libraries do not behave as expect as the channel is "insecure"
- The admin user is **deleted** at the first run. This means that after running the container the first time, it is mandatory to create a new administrative user, setting its password and restart the container to verify that it works correctly. Omitting this could cause the total loss of the data configured in Keycloak

### Docker on WSL2

Docker desktop on WSL2 saves the state of the container in a weird-named folder

```
\\wsl$\docker-desktop-data\version-pack-data\community\docker\overlay2\
```

which is then followed by a very long string like this:

```
5369ec01fbfc3a079a8e3a8d7966fc138a050138738f3eb6bb7fb265f80a763a
```

This string can be obtained  at creation time when running `docker create` instead of `docker run` (which is a shortcut for `docker create` + `docker start`). In other words, never use `docker run` but run `docker create` and save the returned string.

## Saving the state of the container

The very first thing to do is prepare a folder in Windows where the container will save the data. This opens the possibility to completely delete the container and start with a new one without saving data.

Keycloak can work with several databases but we don't need `Postgre` or `SQL Server` in development, we can simply rely on the internal database called `H2`. The default database is not a good choice for development purposes but it is more than enough for development.

When using `H2`, Keycloak version 19 saves all the data in the folder `/opt/keycloak/data`.

Follow these steps:

1. Create a folder on the host machine

```
md H:\VMs\ContainerData\Keycloak19
```

2. Create the container. The `-v` option maps the Windows folder to the `data` folder of the container. The `--name` assign the container a friendly name.

```
docker create -p 8080:8080 -e KEYCLOAK_USER=admin -e KEYCLOAK_PASSWORD=admin -v H:\VMs\ContainerData\Keycloak19:/opt/keycloak/data --name KeycloakDev19 quay.io/keycloak/keycloak:19.0.1 start-dev
```

3. Once created, the docker CLI output the following string, as described in the `WSL2` section before.

```
9a68e1cdea0ddb70b0250b71572693f131993beed9516f0588b2dbd5d206e2ca
```


This procedure is compatible with both the standard and the custom container images.

## A better docker image

In order to resolve all the problems discussed before, we can instead create a custom image with `docker build`.

Since we want to use `https`, we need the certificates to inject inside the container. We also want the **private key** of the certificates which are mandatory if you'll ever want to sniff the conversation between your application and Keycloak from Wireshark. For this reason, we cannot  run the Java `keytool` as suggested by the Keycloak documentation.

### Creating the certificates for localhost

Create self-signed `pfx` (also known as `p12`) certificate that will be used from both Keycloak and your application in development mode (ASP.NET). These are normal "HTTPS" certificates that must specify the SAN extension to appear good to the modern browsers.

The `pfx` should be password protected; we will pass the password to Keycloak in the environment variables

> The `pfx` format is also known as "store" as it can contain multiple certificates and the private key.

Creating certificates for `localhost` is good enough, but you may want to add more DNS names to the SAN extension so that you can also use them for other hosts. The certificates for this demo are created with a custom tool in .NET but you can use OpenSSL or other tools. Remember that ASP.NET recognize the certificate as good when it has the following extensions "1.3.6.1.5.5.7.3.8" (timestamping), "1.3.6.1.4.1.311.84.1.1" (ASP.NET certificate version - optional) and "1.3.6.1.5.5.7.3.1" (Server authentication). The password for the certificates in this folder is "password" and, of course, should never be used in production.

At least you should install the network address `host.docker.internal` to the SAN names if you want to use the "Backchannel" defined by the oAuth specifications.

### Installing the certificates in Windows

Windows needs these certificates for two reasons:

1. We want to use the Keycloak admin console without Chrome complaining
2. We want our ASP.NET application to authenticate our test users on a fully safe HTTPS channel to avoid any restriction from both Keycloak and the security libraries

The pfx certificate (the one that contains the private key) should be installed in both the "Trusted Root Certification Authorities" and "Personal" (current user is ok).

### Preparing a custom image

Preparing a custom image is just a matter of creating the `dockerfile` and running the `docker build` command.

The `dockerfile` is expected to be in the same folder where the certificates are saved.

```
FROM quay.io/keycloak/keycloak:19.0.1 as builder

ENV KC_HEALTH_ENABLED=true
ENV KC_METRICS_ENABLED=true
ENV KC_FEATURES=token-exchange,declarative-user-profile
# ENV KC_DB=postgres  # we run with H2
# Install custom providers
RUN curl -sL https://github.com/aerogear/keycloak-metrics-spi/releases/download/2.5.3/keycloak-metrics-spi-2.5.3.jar -o /opt/keycloak/providers/keycloak-metrics-spi-2.5.3.jar
RUN /opt/keycloak/bin/kc.sh build

FROM quay.io/keycloak/keycloak:19.0.1
COPY --from=builder /opt/keycloak/ /opt/keycloak/
WORKDIR /opt/keycloak

### ======================
# install an existent certificate in the java keystore (needed by the Backchannel)
USER root
COPY test.pfx /opt/keycloak/test.p12
COPY test.pem /opt/keycloak/test.pem

RUN keytool -importcert -alias localhost -cacerts -storepass changeit -noprompt -trustcacerts -file /opt/keycloak/test.pem
# if the certificate is password protected, add:
# -srcstorepass password
RUN keytool -importkeystore -srckeystore test.p12 -srcstoretype PKCS12 -destkeystore conf/server.keystore -deststorepass password
### ======================

# The certificate must be in the folder shared with Windows
ENV KC_HTTPS_KEY_STORE_FILE=/opt/keycloak/data/test.pfx
ENV KC_HTTPS_KEY_STORE_PASSWORD=password

# change these values to point to a running postgres instance
#ENV KC_DB_URL=<DBURL>
#ENV KC_DB_USERNAME=<DBUSERNAME>
#ENV KC_DB_PASSWORD=<DBPASSWORD>
#ENV KC_HOSTNAME=localhost

# let the dynamic resolution of the host name do its work
ENV KC_HOSTNAME_STRICT=false

# disable HSTS
ENV KC_HOSTNAME_STRICT_HTTPS=false
ENV KC_HTTP_ENABLED=true
ENTRYPOINT ["/opt/keycloak/bin/kc.sh", "--verbose"]
```

Let's see the detail for those options:

* All the database environment variables are commented out because we will use the internal H2 database. In production we may want to use `Postgre` or one of the other databases supported by Keycloak

* The first half of the `dockerfile` is standard. It is worth noting the enabled features at the very top of the file. Other options can be similarly enabled. For example, to enable `declarative-user-profile` the above line becomes:

  ```
  ENV KC_FEATURES=token-exchange,declarative-user-profile
  ```

* `KC_HTTPS_KEY_STORE_FILE` specify the full path of the `pfx` certificate file. Later we will see that this path is virtual and points to a location that is accessible from Windows (the host).

* `KC_HTTPS_KEY_STORE_PASSWORD` is the password used during the creation of the certificate file.

* `KC_HOSTNAME` is commented out and should **not** be set.

* `KC_HOSTNAME_STRICT=false` in combination with the absence of `KC_HOSTNAME` is the only way to use the local admin interface with different DNS names. You may want to access Keycloak using http://localhost:8080 and `https://yourpc:8443` (provided that `yourpc` is part of the SAN names in the certificate).

* `KC_HOSTNAME_STRICT_HTTPS=false` This is the variable used to control HSTS (by default is on). The Keycloak version 19.0.1 has a bug  preventing HSTS to be disabled.

> HSTS is a mechanism used by servers to tell the browser to always use HTTPS for a specific domain, regardless the port number.  Once HSTS has been issued, you can remove it by visiting the page [chrome://net-internals/#hsts](chrome://net-internals/#hsts). 
>
> You can check if the domain is under the HSTS policy using the `Query HSTS/PKP domain` section. You can delete the HSTS policy in the `Delete domain security policies`.

* `KC_HTTP_ENABLED=true` This enables HTTP which is disabled by default. You will want to use the admin console via HTTP and not HTTPS to avoid HSTS on localhost. Setting HSTS on localhost will prevent you to visit **any** page of localhost, regardless the port. Since you are a developer, this will cause a lot of problem.
* `ENTRYPOINT ["/opt/keycloak/bin/kc.sh", "--verbose"]` This will start Keycloak when the container is started. The `--verbose` option can be precious because Keycloak will show additional details in the container terminal (you can see it by selecting the container in the docker desktop for Windows).

### Cooking the container image

To create the image, we use the following command where `-t` is used to tag the image with the given name.

```
docker build . -t keycloak19.0.1_local
```

Running multiple times the build command is fine. The command will replace the tag for the given image if it already exists. The old images will be orphaned (no tag) and can be deleted safely.

### Creating the container

We are going to create a container that uses a shared state. This means that we can safely delete the container and create a new one without losing the saved data.

In this example:

- `-p host:container` remap the host port to the container port

- `-e` set an environment variable

- `--name` set the name of the container

- `-v` Bind mount a volume:

  - `H:\VMs\ContainerData\Keycloak19` is the Windows folder containing the state

  - `/opt/keycloak/data` is the linux folder inside the container that is virtualized

- `keycloak19.0.1_local` is the name of the image created with `docker build`

- `start --optimized` is the command that will be issued to run Keycloak as soon as the container is started

```
docker create -p 8080:8080 -p 8443:8443 -e KEYCLOAK_USER=admin -e KEYCLOAK_PASSWORD=admin -v H:\VMs\ContainerData\Keycloak19:/opt/keycloak/data --name KeycloakDev19 keycloak19.0.1_local start --optimized
```

The `pfx` certificate must be copied inside the `H:\VMs\ContainerData\Keycloak19` folder and will be seen by Keycloak inside the `/opt/keycloak/data` directory.

### Starting the container

Now we can start the container and see the logs from the docker desktop detail window.

### Adding an admin

It is very important adding a secondary admin user because the default user `admin` may be deleted from Keycloak because it is the default user with an insecure password.

To add a secondary admin:

- Select the realm / Users tab
- Create a new user
- To be an admin, you must open `Role mapping` and **add all the roles** 

### Links

* Environment variables: https://www.keycloak.org/server/all-config

  The environment variables are show expanding each item on the `>` mark

* TLS configuration: https://www.keycloak.org/server/enabletls

* Java needs the certificates in its keystore (used only for calls originating  from Keycloak such as the Backchannel): https://confluence.atlassian.com/kb/unable-to-connect-to-ssl-services-due-to-pkix-path-building-failed-error-779355358.html

---

## Running a local development mail server

For this purpose `mailhog` does the job.

We can install and run the `mailhog` mail server container with this command:

```
docker run -p 8025:8025 -p 1025:1025 mailhog/mailhog
```

In this case, losing all the email sent for test is not an issue. 

> If you see a Chrome `ERR_SSL_PROTOCOL_ERROR` it means you have HSTS enabled for localhost. In this case:
>
> 1. Remove HSTS for localhost using the Chrome aforementioned settings page [chrome://net-internals/#hsts](chrome://net-internals/#hsts)
> 2. Don't open Keycloak administrative page under HTTPS, but just use HTTP
> 3. Reopen the local HTTP page for `mailhog` and this time it should work

