# MQ_Poc use guid
## woker 參數介紹

`RabbitMqWorkerBase`是MQ架構中的抽象類別，提供連線還有關機後等事情，邏輯實現交由實現子類別來處理

本次題目要利用 Gruop 來分群處理請求，我們可以透過`RabbitMqSetting.ThreadSettings`來設定

* WorkUnitCount:此 ThreadPool 提供幾個 Thread 來處理
* Group:哪一個群組

```c#
ThreadSettings = new ThreadSetting[] //which can read from setting files.
{
    new ThreadSetting(){WorkUnitCount = 3,Group = "groupA"},
    new ThreadSetting(){WorkUnitCount = 3,Group = "groupB"}
}
```

## Rrunning by docker-compose

執行前請先把CMD路徑設定到本專案根目錄

使用 `docker-compose` 執行container.

```cmd
docker-compose --env-file .\env\.env up -d
```

執行完後，會啟動一個MQ Server、publisher、nodeworker

如果想要scale out publisher

```cmd
docker-compose --env-file .\env\.env  up -d --scale publisher=2 --no-recreate
```

如果想要scale out nodeworker

```cmd
docker-compose --env-file .\env\.env  up -d --scale nodeworker=2 --no-recreate
```

### RabbitMQ 連接資訊

RabbitMQ站台連接資訊

* url : http://localhost:8888/
* user : guest
* account : guest

### 環境參數

目前在 `.\env\.env` 有環境參數檔案可以注入 `docker-compose` 中

```env
RABBITMQ_HOSTNAME=rabbitmq-server
QUEUENAME=worker-queue
RABBITMQ_PORT=5672
```

* RABBITMQ_HOSTNAME:rabbitMq server 主機名稱
* RABBITMQ_PORT:rabbitMq server port
* QUEUENAME:使用queue名稱

> Worker 支援 Graceful shutdown所以大膽地做Scale out

### ProcessPool docker-compose

目前支援使用 Process Pool 透過`docker-compose-process.yml`來執行 process Pool 版本

在 nodeworker 要加上以下環境變數

* `POOL_TYPE`：
  * 0 ThreadPool(default)
  * 1 ProcessPool
* `DBConnection`：來串接Dashboard

```yaml
environment:
- POOL_TYPE=1
- DBConnection=Data Source=sqlserver;Initial Catalog=orleans;User ID=sa;Password=test.123;
```

proceess Pool 支援 Dashboard 來查看請求狀態 透過可以查看`http://localhost:8899/`

> 帳密 admin/test.123

```bash
docker-compose --env-file .\env\.env -f .\docker-compose-process.yml up -d 

docker-compose --env-file .\env\.env -f .\docker-compose-process.yml  up -d --scale publisher=4 --no-recreate
```

## Running by k3d

執行前請先把 CMD 路徑設定到本專案根目錄，並且依照下面指示步驟依序往下動作

我們利用 k3d 建立一個 k8s 在 local container 中

```cmd
k3d cluster create my-k3d -p "8888:80@loadbalancer"
```

### 設定 private registry

```cmd
kubectl create secret docker-registry app-docker-dev --docker-server=docker.io --docker-username=<user_name> --docker-password=<user_password>
```

> `<user_name>` & `<user_password>` 輸入 login `docker.io` registry 帳密(AD帳密)

### 設定 configmap & secret

```cmd
kubectl apply -f  ./k8s/mq-poc-secret.yaml
kubectl apply -f  ./k8s/mq-poc-configmap.yaml
```

### 安裝 rabbitmq cluster-operator

```cmd
kubectl apply -f https://github.com/rabbitmq/cluster-operator/releases/download/v1.14.0/cluster-operator.yml
```

```cmd
kubectl apply -f  ./k8s/rabbitmq-cluster-operator.yaml
```

### 建立 Publisher

```cmd
kubectl apply -f  ./k8s/mq-poc-publisher.yaml
```

### 建立 Worker

```cmd
kubectl apply -f  ./k8s/mq-poc-worker.yaml
```

### 建立 ingress

建立 ingress 對外暴露Rabbitmq

```cmd
kubectl apply -f  .\k8s\mq-poc-ingress.yaml
```

執行完以上動作後就可以看到 k8s 上跑起我們 Worker & Publisher

k8s scale publisher

```cmd
kubectl scale --replicas=8 -f .\k8s\mq-poc-publisher.yaml  
```

k8s scale worker

```cmd
kubectl scale --replicas=3 -f .\k8s\mq-poc-worker.yaml
```

## All in one apply by powershell.

```
.\k8s\apply-k8s-all.ps1     
```