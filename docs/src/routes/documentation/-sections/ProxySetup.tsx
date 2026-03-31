import { Settings } from "lucide-react";
import { useTranslation } from "react-i18next";
import {
	CodeBlock,
	Paragraph,
	SectionHeader,
} from "../../../components/documentation";

export const ProxySetup = () => {
	const { t } = useTranslation();

	return (
		<section id="proxy-setup">
			<SectionHeader icon={Settings} title={t("getting-started:proxy.title")} />
			<div className="space-y-4">
				<Paragraph translationKey="getting-started:proxy.description" />

				<CodeBlock label="Traefik">{`labels:
  traefik.enable: true
  traefik.http.routers.boardgametracker-rtr.entrypoints: websecure
  traefik.http.routers.boardgametracker-rtr.rule: Host(\`games.example.com\`)
  traefik.http.routers.boardgametracker-rtr.middlewares: local-only@file
  traefik.http.routers.boardgametracker-rtr.service: boardgametracker-svc
  traefik.http.services.boardgametracker-svc.loadbalancer.server.port: 5444`}</CodeBlock>
			</div>
		</section>
	);
};
