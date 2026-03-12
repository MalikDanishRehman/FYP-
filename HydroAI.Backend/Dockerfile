# Python ka halka aur fast server
FROM python:3.11-slim

# Container ke andar folder set karna
WORKDIR /app

# Requirements copy karna aur install karna
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Baqi ka saara Python code copy karna
COPY . .

# Port open karna jahan FastAPI chalegi
EXPOSE 8000

# Server ko start karne ki command
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]