import { useQuery } from "@tanstack/react-query";
import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Database from "@/assets/icons/database.svg?react";
import Keyboard from "@/assets/icons/keyboard.svg?react";
import MagnifyingGlass from "@/assets/icons/magnifying-glass.svg?react";
import BgtBigButton from "@/components/BgtButton/BgtBigButton";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtStatus } from "@/components/BgtStatus/BgtStatus";
import { usePermissions } from "@/hooks/usePermissions";
import { isIntegrationFailure } from "@/models/Settings/IntegrationStatus";
import { RequireWrite } from "@/routes/-components/RequireWrite";
import { useBack } from "@/routes/-hooks/useBack";
import { useSettingsData } from "@/routes/settings/-hooks/useSettingsData";
import { getIntegrationStatuses } from "@/services/queries/settings";

export const Route = createFileRoute("/games/add")({
	component: WriteRouteComponent,
});

function RouteComponent() {
	const { t } = useTranslation(["game", "common", "settings"]);
	const navigate = useNavigate();
	const goBack = useBack("/games");
	const { settings } = useSettingsData();

	const { canManageSettings } = usePermissions();

	const bggEnabled = settings?.bggStatus?.isConfigured ?? false;
	const { data: integrationStatuses } = useQuery({
		...getIntegrationStatuses(),
		enabled: bggEnabled && canManageSettings,
	});
	const bggConnection = integrationStatuses?.find((status) => status.integration === "bgg");

	return (
		<BgtPage>
			<BgtPageHeader backAction={goBack} backText={t("common:back")} header={t("game:new.title")} />
			<BgtPageContent className="items-center">
				<BgtCard className="w-full max-w-xl">
					<p className="text-cancel mb-4">{t("game:new.description")}</p>
					<div className="flex flex-col gap-4 mb-6">
						{settings && !bggEnabled && (
							<BgtStatus
								variant="warning"
								title={t("settings:bgg.status.not-configured")}
								description={
									<>
										{t("game:new.bgg-missing.description")}{" "}
										<Link
											to="/settings"
											search={{ tab: "bgg" }}
											className="font-medium text-white underline hover:text-orange-300"
										>
											{t("game:new.bgg-missing.link")}
										</Link>
									</>
								}
							/>
						)}

						{bggEnabled && isIntegrationFailure(bggConnection) && (
							<BgtStatus
								variant="warning"
								title={t(`settings:integrations.status.${bggConnection.status}`)}
								description={
									<>
										{t(`settings:integrations.reason.${bggConnection.messageKey}`)}{" "}
										<Link
											to="/settings"
											search={{ tab: "bgg" }}
											className="font-medium text-white underline hover:text-orange-300"
										>
											{t("game:new.bgg-missing.link")}
										</Link>
									</>
								}
							/>
						)}

						<BgtBigButton
							title={t("game:new.bgg-title")}
							subText={t("game:new.bgg-subtext")}
							icon={MagnifyingGlass}
							onClick={() => navigate({ to: "/games/bgg" })}
							disabled={!bggEnabled}
						/>
						<BgtBigButton
							title={t("game:new.manual-title")}
							subText={t("game:new.manual-subtext")}
							icon={Keyboard}
							onClick={() => navigate({ to: "/games/new" })}
						/>
						<BgtBigButton
							title={t("game:new.bgg-import-title")}
							subText={t("game:new.bgg-import-subtext")}
							icon={Database}
							onClick={() => navigate({ to: "/games/import/start" })}
							disabled={!bggEnabled}
						/>
					</div>
					<BgtButton variant="cancel" className="w-full" onClick={goBack}>
						{t("common:cancel")}
					</BgtButton>
				</BgtCard>
			</BgtPageContent>
		</BgtPage>
	);
}

function WriteRouteComponent() {
	return (
		<RequireWrite>
			<RouteComponent />
		</RequireWrite>
	);
}
