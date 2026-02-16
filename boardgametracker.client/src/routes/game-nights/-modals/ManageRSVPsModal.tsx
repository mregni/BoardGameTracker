import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtDialog, BgtDialogClose, BgtDialogContent, BgtDialogTitle } from "@/components/BgtDialog";
import { BgtText } from "@/components/BgtText/BgtText";
import { type GameNight, GameNightRsvpState, type ModalProps, type UpdateGameNightRsvp } from "@/models";
import { RsvpStatBadge } from "../-components/RsvpStatBadge";
import { RsvpStatusButton } from "../-components/RsvpStatusButton";

interface Props extends ModalProps {
	gameNight: GameNight | null;
	onUpdateRsvp: (rsvp: UpdateGameNightRsvp) => Promise<unknown>;
	isLoading: boolean;
}

export const ManageRSVPsModal = (props: Props) => {
	const { open, close, gameNight, onUpdateRsvp, isLoading } = props;
	const { t } = useTranslation();

	if (!gameNight) return null;

	const handleStatusChange = async (rsvpId: number, status: GameNightRsvpState) => {
		await onUpdateRsvp({
			id: rsvpId,
			gameNightId: gameNight.id,
			playerId: 0,
			state: status,
		});
	};

	const acceptedCount = gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Accepted).length;
	const pendingCount = gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Pending).length;
	const declinedCount = gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Declined).length;

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent className="max-w-3xl!">
				<BgtDialogTitle>{t("game-nights.rsvp.title")}</BgtDialogTitle>
				<div className="flex flex-col gap-4">
					<div className=" bg-blue-500/10 border border-blue-500/20 rounded-lg p-4">
						<BgtText size="2" color="blue">
							<strong>{t("game-nights.rsvp.manual-note-prefix")}</strong> {t("game-nights.rsvp.manual-note")}
						</BgtText>
					</div>

					<div className="flex flex-col gap-4">
						{[...gameNight.invitedPlayers]
							.sort((a, b) => (a.playerId === gameNight.hostId ? 1 : b.playerId === gameNight.hostId ? -1 : 0))
							.map((rsvp) => {
								const isHost = rsvp.playerId === gameNight.hostId;
								return (
									<div key={rsvp.id} className="bg-white/5 border border-white/10 rounded-lg p-4">
										<div className="flex items-center justify-between flex-row gap-4 flex-wrap">
											<div className="flex items-center gap-3">
												<BgtAvatar title={rsvp.player.name} image={rsvp.player.image} size="large" />
												<BgtText size="3" weight="medium">
													{rsvp.player.name}
													{isHost && <span className="ml-2 text-xs text-gray-400">({t("game-nights.rsvp.host")})</span>}
												</BgtText>
											</div>
											<div className="flex gap-2">
												<RsvpStatusButton
													rsvpId={rsvp.id}
													state={GameNightRsvpState.Accepted}
													onStatusChange={handleStatusChange}
													disabled={isLoading || isHost}
													isActive={rsvp.state === GameNightRsvpState.Accepted}
													variant="green"
													label={t("game-nights.rsvp.accept")}
												/>
												<RsvpStatusButton
													rsvpId={rsvp.id}
													state={GameNightRsvpState.Pending}
													onStatusChange={handleStatusChange}
													disabled={isLoading || isHost}
													isActive={rsvp.state === GameNightRsvpState.Pending}
													variant="yellow"
													label={t("game-nights.rsvp.maybe")}
												/>
												<RsvpStatusButton
													rsvpId={rsvp.id}
													state={GameNightRsvpState.Declined}
													onStatusChange={handleStatusChange}
													disabled={isLoading || isHost}
													isActive={rsvp.state === GameNightRsvpState.Declined}
													variant="red"
													label={t("game-nights.rsvp.decline")}
												/>
											</div>
										</div>
									</div>
								);
							})}
					</div>

					<div className="grid grid-cols-3 gap-4 mb-6">
						<RsvpStatBadge count={acceptedCount} label={t("common.accepted")} variant="green" />
						<RsvpStatBadge count={pendingCount} label={t("common.pending")} variant="amber" />
						<RsvpStatBadge count={declinedCount} label={t("common.declined")} variant="red" />
					</div>
				</div>

				<BgtDialogClose>
					<BgtButton variant="primary" className="flex-1" onClick={close}>
						{t("common.done")}
					</BgtButton>
				</BgtDialogClose>
			</BgtDialogContent>
		</BgtDialog>
	);
};
