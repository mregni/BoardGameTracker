import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import BgtButton from "@/components/BgtButton/BgtButton";
import { useModalState } from "@/hooks/useModalState";
import { usePermissions } from "@/hooks/usePermissions";
import type { GameNight, Player } from "@/models";
import { useSettingsData } from "@/routes/settings/-hooks/useSettingsData";
import { sendInvitesCall } from "@/services/gameNightService";
import { apiErrorMessage } from "@/utils/errorUtils";
import { SendInvitesModal } from "../-modals/SendInvitesModal";
import { inviteRecipients, inviteState } from "../-utils/inviteRecipients";

interface Props {
	gameNight: GameNight;
	players: Player[];
}

export const EmailInvitesButton = ({ gameNight, players }: Props) => {
	const { t } = useTranslation("game-nights");
	const { settings } = useSettingsData();
	const { isAdmin } = usePermissions();
	const { blocker } = inviteState(settings, isAdmin);
	const confirmModal = useModalState();

	const sendInvitesMutation = useMutation({
		mutationFn: () => sendInvitesCall(gameNight.id),
		onSuccess: (result) => {
			confirmModal.hide();
			toast.success(t("card.invites-sent", { sent: result.sent }));
		},
		onError: (error) => {
			toast.error(apiErrorMessage(error, "game-nights:card.invites-failed"));
		},
	});

	if (blocker !== null) return null;

	return (
		<>
			<BgtButton variant="cancel" size="2" onClick={confirmModal.show}>
				{t("card.email-invites")}
			</BgtButton>
			{confirmModal.isOpen && (
				<SendInvitesModal
					open={confirmModal.isOpen}
					close={confirmModal.hide}
					title={gameNight.title}
					recipients={inviteRecipients(gameNight, players)}
					isPending={sendInvitesMutation.isPending}
					onConfirm={() => sendInvitesMutation.mutate()}
				/>
			)}
		</>
	);
};
