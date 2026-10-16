import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { isPast } from "date-fns";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import AlertTriangle from "@/assets/icons/alert-triangle.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { BgtText } from "@/components/BgtText/BgtText";
import { useAuth } from "@/hooks/useAuth";
import { toWeekdayDisplay } from "@/utils/dateUtils";
import { useSettingsData } from "../settings/-hooks/useSettingsData";
import { RsvpEventDetails } from "./-components/RsvpEventDetails";
import { RsvpGames } from "./-components/RsvpGames";
import { RsvpResponseForm } from "./-components/RsvpResponseForm";
import { RsvpSuccessView } from "./-components/RsvpSuccessView";
import { RsvpWhosComing } from "./-components/RsvpWhosComing";
import { useRsvpData } from "./-hooks/useRsvpData";

const rsvpSearchSchema = z.object({
	linkId: z.string(),
});

export const Route = createFileRoute("/_bare/rsvp")({
	component: RsvpPage,
	validateSearch: rsvpSearchSchema,
});

function RsvpPage() {
	const { settings } = useSettingsData();
	const { t } = useTranslation(["rsvp", "auth"]);
	const navigate = useNavigate();
	const { linkId } = Route.useSearch();
	const isAuthenticated = useAuth((s) => s.isAuthenticated);
	const authStatus = useAuth((s) => s.authStatus);
	const { gameNight, manuals, isLoading, answer, isEditing, submitRsvp, isSubmitting, changeResponse } =
		useRsvpData(linkId);

	const requiresAuth = settings?.rsvpAuthenticationEnabled && authStatus?.authEnabled && !isAuthenticated;

	if (requiresAuth) {
		return (
			<BgtEmptyPage
				showHeader={false}
				icon={AlertTriangle}
				title={t("auth:rsvp-login-required")}
				description={t("auth:rsvp-login-description")}
				action={{
					label: t("auth:login"),
					onClick: () => navigate({ to: "/login", search: { redirect: `/rsvp?linkId=${linkId}` } }),
				}}
			/>
		);
	}

	if (!isLoading && !gameNight) {
		return (
			<BgtEmptyPage
				showHeader={false}
				icon={AlertTriangle}
				title={t("not-found")}
				description={t("not-found-description")}
			/>
		);
	}

	return (
		<BgtPage>
			<BgtPageContent isLoading={isLoading} data={{ gameNight, settings }} className="mx-auto w-full max-w-2xl">
				{({ gameNight, settings }) => {
					if (isPast(gameNight.startDate)) {
						return (
							<BgtCard className="gap-1.5" data-testid="rsvp-past">
								<BgtHeading as="h1" size="5" className="wrap-anywhere">
									{t("past-title", { title: gameNight.title })}
								</BgtHeading>
								<BgtText size="2" color="gray" className="first-letter:uppercase">
									{toWeekdayDisplay(gameNight.startDate, settings.dateFormat, settings.uiLanguage)}
								</BgtText>
								<BgtText size="2">{t("past-description")}</BgtText>
							</BgtCard>
						);
					}

					const answeredPlayer = gameNight.invitedPlayers.find((rsvp) => rsvp.playerId === answer?.playerId);

					return (
						<>
							<RsvpEventDetails gameNight={gameNight} settings={settings} />
							<RsvpWhosComing gameNight={gameNight} />
							{answer && answeredPlayer && !isEditing ? (
								<RsvpSuccessView
									playerName={answeredPlayer.player.name}
									response={answer.state}
									hostNotified={settings.emailEnabled && answeredPlayer.playerId !== gameNight.hostId}
									onChangeResponse={changeResponse}
								/>
							) : (
								<RsvpResponseForm
									invitedPlayers={gameNight.invitedPlayers}
									hostId={gameNight.hostId}
									initialAnswer={answer}
									onSubmit={submitRsvp}
									isSubmitting={isSubmitting}
								/>
							)}
							<RsvpGames linkId={linkId} games={gameNight.suggestedGames} manuals={manuals} />
						</>
					);
				}}
			</BgtPageContent>
		</BgtPage>
	);
}
