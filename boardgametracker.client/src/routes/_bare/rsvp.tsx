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
import { useDocumentTitle } from "@/hooks/useDocumentTitle";
import { isNightInProgress } from "@/routes/game-nights/-utils/gameNightPlay";
import { toCalendarHref } from "@/utils/calendarFile";
import { toDisplay, toDisplayTime, toDisplayWithWeekday } from "@/utils/dateUtils";
import { effectivePublicUrl } from "@/utils/publicUrl";
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
	useDocumentTitle(gameNight?.title);

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
						const inProgress = isNightInProgress(gameNight);
						return (
							<BgtCard className="gap-1.5" data-testid={inProgress ? "rsvp-in-progress" : "rsvp-past"}>
								<BgtHeading as="h1" size="5" className="wrap-anywhere">
									{inProgress
										? t("in-progress-title", { title: gameNight.title })
										: t("past-title", { title: gameNight.title })}
								</BgtHeading>
								<BgtText size="2" color="gray">
									{toDisplay(gameNight.startDate, settings.dateFormat, settings.uiLanguage)}
								</BgtText>
								<BgtText size="2">{inProgress ? t("in-progress-description") : t("past-description")}</BgtText>
							</BgtCard>
						);
					}

					const answeredPlayer = gameNight.invitedPlayers.find((rsvp) => rsvp.playerId === answer?.playerId);

					return (
						<>
							<RsvpEventDetails gameNight={gameNight} settings={settings} />
							{answer && answeredPlayer && !isEditing ? (
								<RsvpSuccessView
									playerName={answeredPlayer.player.name}
									response={answer.state}
									hostNotified={answer.hostNotified}
									whenAndWhere={[
										toDisplayWithWeekday(gameNight.startDate, settings.dateFormat, settings.uiLanguage),
										toDisplayTime(gameNight.startDate, settings.timeFormat, settings.uiLanguage),
										gameNight.location.name,
									].join(" · ")}
									calendarHref={toCalendarHref({
										uid: `${gameNight.linkId}@boardgametracker`,
										title: gameNight.title,
										start: gameNight.startDate,
										location: gameNight.location.name,
										description: gameNight.notes,
										url: `${effectivePublicUrl(settings.publicUrl, window.location.origin)}/rsvp?linkId=${linkId}`,
									})}
									calendarFileName={`${gameNight.title}.ics`}
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
							<RsvpWhosComing gameNight={gameNight} />
						</>
					);
				}}
			</BgtPageContent>
		</BgtPage>
	);
}
