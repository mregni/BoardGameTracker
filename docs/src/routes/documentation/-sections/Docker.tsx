import { Container } from "lucide-react";
import {
	SectionHeader,
	Paragraph,
	CodeBlock,
	SubHeader,
} from "../../../components/documentation";
import { useTranslation } from "react-i18next";

export const Docker = () => {
	const { t } = useTranslation();

	return (
		<section id="docker">
			<SectionHeader
				icon={Container}
				title={t("getting-started:docker.title")}
			/>
			<div className="space-y-4">
				<SubHeader content={t("getting-started:docker.compose.title")} />
				<Paragraph translationKey="getting-started:docker.compose.description" />
				<CodeBlock label="Terminal">
					{`services:
  boardgametracker:
    image: uping/boardgametracker:latest
    restart: unless-stopped
    depends_on:
      db:
        condition: service_healthy
    volumes:
      - ./config:/app/config
      - ./logs:/app/logs
    ports:
      - "5444:5444"
    environment:
      - DB_HOST=db
      - DB_USER=dbuser
      - DB_PASSWORD=CHANGEME        # Change me!!!!
      - DB_NAME=boardgametracker
      - DB_PORT=5432
      - JWT_SECRET=CHANGEME         # Change me!!!!
      - TZ=UTC
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5444/api/health"]
      interval: 30s
      timeout: 5s
      retries: 3
      start_period: 10s

  db:
    image: postgres:16
    restart: unless-stopped
    volumes:
      - ./postgres-data:/var/lib/postgresql/data
    environment:
      - POSTGRES_DB=boardgametracker
      - POSTGRES_USER=dbuser
      - POSTGRES_PASSWORD=CHANGEME # Change me!!!!
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U dbuser -d boardgametracker"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 10s`}
				</CodeBlock>

				<SubHeader content={t("getting-started:docker.run.title")} />
				<Paragraph translationKey="getting-started:docker.run.description" />
				<CodeBlock label="Terminal">{String.raw`docker run -d \
    --name boardgametracker \
    --restart unless-stopped \
    -p 5444:5444 \
    -v ./config:/app/config \
    -v ./logs:/app/logs \
    -e DB_HOST=db \
    -e DB_USER=dbuser \
    -e DB_PASSWORD=CHANGEME \
    -e DB_NAME=boardgametracker \
    -e DB_PORT=5432 \
    -e JWT_SECRET=CHANGEME \
    -e TZ=UTC \
    uping/boardgametracker:latest`}</CodeBlock>
			</div>
		</section>
	);
};
