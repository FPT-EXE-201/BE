# Backend Deployment Guide — FPT EXE 201

> **Mục đích**: Tài liệu bàn giao quy trình deploy Backend lên Ubuntu server qua GitHub Actions + Docker + GHCR.  
> **Phạm vi**: Áp dụng cho backend .NET 8 trong repo này.  
> **Bảo mật**: Tài liệu này chỉ dùng placeholder, tuyệt đối không ghi secret thật vào repo.

---

## 1. Kiến Trúc Deploy Hiện Tại

Luồng deploy production hiện tại:

```text
Developer push code lên GitHub
    ↓
GitHub Actions chạy CI/CD
    ↓
Build solution .NET 8
    ↓
Build Docker image từ Dockerfile
    ↓
Push image lên GHCR
    ↓
SSH vào Ubuntu server
    ↓
Pull image mới nhất
    ↓
Stop/remove container cũ
    ↓
Run container mới với file .env trên server
```

Workflow đang nằm tại `.github/workflows/ci-cd.yml`.

---

## 2. Thành Phần Chính

| Thành phần | Vai trò |
|-----------|---------|
| GitHub Actions | Build, push image, SSH deploy |
| GHCR (`ghcr.io`) | Docker registry lưu image backend |
| Ubuntu server | Chạy container production |
| Docker | Runtime cho backend |
| `.env` trên server | Chứa biến môi trường production |

---

## 3. Điều Kiện Tiên Quyết

Phía vận hành hoặc outsource cần chuẩn bị:

1. Một Ubuntu server có Docker.
2. Một GitHub repository đã bật Actions.
3. Một GitHub account hoặc machine user có quyền pull package từ GHCR.
4. SSH access vào server bằng private key.
5. Domain hoặc IP public trỏ đúng vào server.

---

## 4. Quy Ước Port Hiện Tại

Ví dụ cấu hình production hiện tại:

| Loại port | Giá trị mẫu | Ghi chú |
|-----------|-------------|---------|
| SSH port server | `24460` | Dùng để GitHub Actions SSH vào server |
| App port trong container | `8080` | Backend .NET lắng nghe trong container |
| App port publish ra ngoài | `24466` | Port public để FE hoặc client gọi API |

Mapping thực tế:

```text
Host 24466 -> Container 8080
```

Điều này có nghĩa là backend có thể được truy cập từ ngoài qua:

```text
http://<domain-hoac-ip>:24466
```

Health endpoint:

```text
http://<domain-hoac-ip>:24466/health
```

---

## 5. Chuẩn Bị Ubuntu Server

### 5.1 Cài Docker

```bash
sudo apt update
sudo apt install -y docker.io
sudo systemctl enable --now docker
sudo usermod -aG docker <deploy-user>
newgrp docker
```

Kiểm tra:

```bash
docker --version
docker ps
```

### 5.2 Tạo thư mục deploy

Ví dụ dùng thư mục:

```text
/opt/fpt-exe201-api
```

Tạo thư mục:

```bash
sudo mkdir -p /opt/fpt-exe201-api
sudo chown -R <deploy-user>:<deploy-user> /opt/fpt-exe201-api
```

### 5.3 Tạo file `.env` production

Workflow deploy yêu cầu file này phải tồn tại sẵn trên server:

```text
/opt/fpt-exe201-api/.env
```

Tạo file:

```bash
touch /opt/fpt-exe201-api/.env
chmod 600 /opt/fpt-exe201-api/.env
nano /opt/fpt-exe201-api/.env
```

Mẫu tối thiểu:

```dotenv
ASPNETCORE_ENVIRONMENT=Production

ConnectionStrings__DefaultConnection=Server=<db-host>;Database=<db-name>;User=<db-user>;Password=<db-password>;Port=<db-port>;CharSet=utf8mb4;

Jwt__SecretKey=<jwt-secret-at-least-32-chars>
Jwt__Issuer=FPT.EXE201.Api
Jwt__Audience=FPT.EXE201.Client

Supabase__Url=<supabase-url>
Supabase__ServiceRoleKey=<supabase-service-role-key>
Supabase__Storage__BucketName=EXE201
Supabase__Storage__PublicBaseUrl=<supabase-public-base-url>

AI__Gemini__ApiKey=<gemini-api-key>
AI__Gemini__BaseUrl=https://generativelanguage.googleapis.com/v1beta/
AI__Gemini__DefaultModel=gemini-2.5-flash

AI__AzureDocumentIntelligence__Endpoint=<azure-doc-intelligence-endpoint>
AI__AzureDocumentIntelligence__ApiKey=<azure-doc-intelligence-api-key>

Google__ClientId=<google-client-id>
Swagger__Enabled=true
```

Nguyên tắc:

1. Không commit file `.env` production vào Git.
2. Không dán secret thật vào tài liệu nội bộ công khai.
3. Chỉ lưu `.env` trên server với quyền truy cập hạn chế.
4. Nếu cần truy cập Swagger UI trên production, đặt `Swagger__Enabled=true` trong file `.env` của server.

Khi bật Swagger ở production, UI mặc định nằm tại:

```text
http://<domain-hoac-ip>:<app-host-port>/swagger
```

---

## 6. Chuẩn Bị SSH Cho GitHub Actions

### 6.1 Tạo SSH key riêng cho deploy

Trên máy quản trị:

```bash
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/github_actions_deploy
```

Kết quả:

| File | Vai trò |
|------|---------|
| `~/.ssh/github_actions_deploy` | Private key, đưa vào GitHub secret |
| `~/.ssh/github_actions_deploy.pub` | Public key, thêm vào server |

### 6.2 Thêm public key vào server

```bash
mkdir -p ~/.ssh
cat ~/.ssh/github_actions_deploy.pub >> ~/.ssh/authorized_keys
chmod 700 ~/.ssh
chmod 600 ~/.ssh/authorized_keys
```

Hoặc copy từ máy local lên server:

```bash
ssh-copy-id -i ~/.ssh/github_actions_deploy.pub -p <ssh-port> <deploy-user>@<server-host>
```

### 6.3 Kiểm tra SSH thủ công

```bash
ssh -i ~/.ssh/github_actions_deploy -p <ssh-port> <deploy-user>@<server-host>
```

Nếu không SSH được thủ công thì GitHub Actions cũng sẽ không deploy được.

---

## 7. GitHub Secrets Bắt Buộc

Repository phải có các secret sau:

| Secret | Mô tả | Ví dụ placeholder |
|--------|------|-------------------|
| `SERVER_HOST` | Domain hoặc IP public của server | `api.example.com` |
| `SERVER_USER` | User SSH deploy | `deploy` |
| `SERVER_PORT` | SSH port | `24460` |
| `SERVER_SSH_KEY` | Private key SSH đầy đủ | `-----BEGIN OPENSSH PRIVATE KEY----- ...` |
| `SERVER_APP_DIR` | Thư mục deploy trên server | `/opt/fpt-exe201-api` |
| `APP_CONTAINER_NAME` | Tên container Docker | `fpt-exe201-api` |
| `APP_HOST_PORT` | Port public của app | `24466` |
| `GHCR_USERNAME` | GitHub username có quyền pull package | `<github-username>` |
| `GHCR_PAT` | GitHub PAT để `docker login ghcr.io` | `<token>` |

### 7.1 Yêu cầu cho `GHCR_PAT`

Khuyến nghị dùng GitHub Personal Access Token có quyền:

```text
read:packages
```

Nếu package private và quyền repo yêu cầu thêm, có thể cần:

```text
repo
```

### 7.2 Yêu cầu cho `SERVER_SSH_KEY`

Phải là **private key**, không phải `.pub`.

Đúng dạng:

```text
-----BEGIN OPENSSH PRIVATE KEY-----
...
-----END OPENSSH PRIVATE KEY-----
```

---

## 8. Workflow CI/CD

### 8.1 Trigger

Workflow chạy khi:

1. Tạo Pull Request vào `main` -> chạy build/validate.
2. Push vào `main` -> build, push image, deploy.
3. Push vào `develop` -> build/validate.
4. Chạy thủ công qua `workflow_dispatch`.

### 8.2 Các job chính

| Job | Vai trò |
|-----|---------|
| `build-and-validate` | Restore, build .NET, build Docker image |
| `publish-image` | Push image lên GHCR |
| `deploy-to-ubuntu` | SSH vào server và chạy lại container |

### 8.3 Quy trình deploy trên server

Job deploy hiện chạy logic tương đương:

```bash
docker login ghcr.io -u <ghcr-username> --password-stdin
docker pull ghcr.io/<owner>/<repo>:latest
docker rm -f <container-name> || true
docker run -d \
  --name <container-name> \
  --restart unless-stopped \
  --env-file <server-app-dir>/.env \
  -p <app-host-port>:8080 \
  ghcr.io/<owner>/<repo>:latest
docker image prune -f
```

---

## 9. Kiểm Tra Sau Deploy

### 9.1 Kiểm tra container

```bash
docker ps
```

Kỳ vọng thấy container backend đang `Up`.

### 9.2 Kiểm tra log

```bash
docker logs -f <container-name>
```

Kỳ vọng có các log tương tự:

```text
Starting FPT.EXE201 API application...
Database is up to date — no pending migrations
Database seeding completed successfully
FPT.EXE201 API started successfully
```

### 9.3 Kiểm tra health endpoint

```bash
curl http://127.0.0.1:<app-host-port>/health
curl http://<domain-hoac-ip>:<app-host-port>/health
```

Kỳ vọng HTTP `200`.

---

## 10. Rollback Thủ Công

Nếu image mới có vấn đề, có thể rollback bằng cách chạy lại container với tag cũ hơn.

Ví dụ:

```bash
docker pull ghcr.io/<owner>/<repo>:sha-<old-sha>
docker rm -f <container-name>
docker run -d \
  --name <container-name> \
  --restart unless-stopped \
  --env-file <server-app-dir>/.env \
  -p <app-host-port>:8080 \
  ghcr.io/<owner>/<repo>:sha-<old-sha>
```

---

## 11. Troubleshooting

### 11.1 SSH deploy fail

Triệu chứng:

```text
unable to authenticate
ssh: no key found
```

Nguyên nhân thường gặp:

1. `SERVER_SSH_KEY` đang là public key thay vì private key.
2. SSH port sai.
3. Server chặn port SSH.
4. Public key chưa được thêm vào `authorized_keys`.

### 11.2 Docker pull fail từ GHCR

Triệu chứng:

```text
unauthorized
denied
```

Nguyên nhân thường gặp:

1. `GHCR_USERNAME` sai.
2. `GHCR_PAT` thiếu quyền `read:packages`.
3. Package private nhưng token không đủ quyền.

### 11.3 App start nhưng health fail

Kiểm tra:

```bash
docker logs -f <container-name>
```

Nguyên nhân thường gặp:

1. File `.env` thiếu biến bắt buộc.
2. Kết nối DB fail.
3. App port mapping sai.

### 11.4 Port conflict

Kiểm tra:

```bash
docker ps --format "table {{.Names}}\t{{.Ports}}"
```

Nếu host port đã bị dùng bởi container khác, đổi `APP_HOST_PORT` hoặc giải phóng port cũ.

---

## 12. Các Cảnh Báo Vận Hành

1. Không commit file `.env` production.
2. Không dán API keys hoặc password thật vào ticket, chat công khai hoặc tài liệu repo.
3. Khi nghi ngờ secret đã lộ, phải rotate ngay.
4. SSH key cho GitHub Actions nên là key riêng, không dùng chung key cá nhân.
5. Nếu cần an toàn cao hơn, nên thêm Nginx + HTTPS phía trước backend.

---

## 13. Checklist Bàn Giao Cho Outsource

Trước khi bàn giao, xác nhận đủ các mục sau:

1. Có quyền vào GitHub repository settings.
2. Có đủ GitHub secrets bắt buộc.
3. SSH vào Ubuntu server bằng key deploy thành công.
4. Docker đã cài trên server.
5. Thư mục deploy đã tồn tại.
6. File `.env` production đã tồn tại trên server.
7. Domain hoặc IP public trỏ đúng về server.
8. SSH port và app port đã mở đúng trên firewall.
9. `curl http://<domain-or-ip>:<app-host-port>/health` trả `200`.

---

## 14. Gợi Ý Cải Tiến Sau Bàn Giao

Các cải tiến nên cân nhắc trong giai đoạn tiếp theo:

1. Thêm bước health check sau deploy ngay trong GitHub Actions.
2. Persist `DataProtection-Keys` ra host volume.
3. Persist logs ra host volume hoặc đẩy lên centralized logging.
4. Đặt reverse proxy Nginx/Caddy phía trước để dùng HTTPS chuẩn.
5. Tách production config và staging config rõ ràng.
