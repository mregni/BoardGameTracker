import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import AlertTriangle from "@/assets/icons/alert-triangle.svg?react";
import PartyPopper from "@/assets/icons/party-popper.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { BgtText } from "@/components/BgtText/BgtText";
import { useAuth } from "@/hooks/useAuth";
import { GameNightRsvpState } from "@/models";
import { RsvpSection } from "@/routes/game-nights/-components/RsvpSection";
import { useSettingsData } from "../settings/-hooks/useSettingsData";
import { RsvpEventDetails } from "./-components/RsvpEventDetails";
import { RsvpResponseForm } from "./-components/RsvpResponseForm";
import { RsvpSuccessView } from "./-components/RsvpSuccessView";
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
	const { t } = useTranslation();
	const navigate = useNavigate();
	const { linkId } = Route.useSearch();
	const { isAuthenticated, authStatus } = useAuth();
	const { gameNight, isLoading, submitRsvp, isSubmitting, isSubmitted, submittedPlayerName, submittedState } =
		useRsvpData(linkId);

	const requiresAuth =
		settings?.rsvpAuthenticationEnabled && authStatus?.authEnabled && !authStatus?.bypassEnabled && !isAuthenticated;

	if (requiresAuth) {
		return (
			<BgtEmptyPage
				header={t("auth.rsvp-login-required")}
				icon={AlertTriangle}
				title={t("auth.rsvp-login-required")}
				description={t("auth.rsvp-login-description")}
			>
				<button
					type="button"
					onClick={() => navigate({ to: "/login" })}
					className="mt-4 py-2 px-6 bg-primary text-white rounded-md font-medium hover:bg-primary/90 transition-colors"
				>
					{t("auth.login")}
				</button>
			</BgtEmptyPage>
		);
	}

	if (isSubmitted) {
		return <RsvpSuccessView playerName={submittedPlayerName} response={submittedState} />;
	}

	if (!isLoading && !gameNight) {
		return (
			<BgtEmptyPage
				header={t("rsvp.not-found")}
				icon={AlertTriangle}
				title={t("rsvp.not-found")}
				description={t("rsvp.not-found-description")}
			/>
		);
	}

	return (
		<BgtPage>
			<BgtPageContent isLoading={isLoading} data={{ gameNight, settings }} className="max-w-3xl mx-auto w-full">
				{({ gameNight, settings }) => {
					const acceptedPlayers = gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Accepted);
					const pendingPlayers = gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Pending);
					const declinedPlayers = gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Declined);

					return (
						<>
							<div className="text-center">
								<div className="inline-flex items-center justify-center w-12 h-12 bg-primary/20 rounded-full mb-3">
									<PartyPopper className="size-6 text-primary" />
								</div>
								<BgtText size="7" weight="bold" color="white" className="block">
									{t("rsvp.youre-invited")}
								</BgtText>
								<BgtText color="gray" size="2">
									{t("rsvp.let-us-know")}
								</BgtText>
							</div>

							<RsvpEventDetails
								gameNight={gameNight}
								timeFormat={settings.timeFormat}
								dateFormat={settings.dateFormat}
							/>

							<RsvpResponseForm
								invitedPlayers={gameNight.invitedPlayers}
								onSubmit={submitRsvp}
								isSubmitting={isSubmitting}
							/>

							<BgtCard>
								<BgtText size="4" weight="bold" className="mb-3">
									{t("rsvp.whos-coming")}
								</BgtText>
								<div className="grid grid-cols-1 md:grid-cols-3 gap-3">
									<RsvpSection
										title={t("common.accepted")}
										count={acceptedPlayers.length}
										players={acceptedPlayers.map((x) => x.player)}
										variant="accepted"
									/>
									<RsvpSection
										title={t("common.pending")}
										count={pendingPlayers.length}
										players={pendingPlayers.map((x) => x.player)}
										variant="pending"
									/>
									<RsvpSection
										title={t("common.declined")}
										count={declinedPlayers.length}
										players={declinedPlayers.map((x) => x.player)}
										variant="declined"
									/>
								</div>
							</BgtCard>
						</>
					);
				}}
			</BgtPageContent>
		</BgtPage>
	);
}
