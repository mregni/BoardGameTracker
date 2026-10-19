import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import { useModalState } from "@/hooks/useModalState";
import { usePermissions } from "@/hooks/usePermissions";
import type { GameNight, Player } from "@/models";
import { useSettingsData } from "@/routes/settings/-hooks/useSettingsData";
import { sendInvitesCall } from "@/services/gameNightService";
import { apiErrorMessage } from "@/utils/errorUtils";
import { useInviteLink } from "../-hooks/useInviteLink";
import { SendInvitesModal } from "../-modals/SendInvitesModal";
import { inviteRecipients, inviteState } from "../-utils/inviteRecipients";

interface Props {
	gameNight: GameNight;
	players: Player[];
	invitable: boolean;
	onEdit: (gameNight: GameNight) => void;
	onDelete: (gameNight: GameNight) => void;
}

export const GameNightActions = (props: Props) => {
	const { gameNight, players, invitable, onEdit, onDelete } = props;
	const { t } = useTranslation("game-nights");
	const { copied, copy } = useInviteLink(gameNight);
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

	return (
		<div className="flex flex-wrap items-center gap-2 md:justify-end">
			{invitable && (
				<BgtButton variant="cancel" size="1" onClick={copy} disabled={copied}>
					{copied ? t("card.copied") : t("card.copy-link")}
				</BgtButton>
			)}
			{invitable && blocker === null && (
				<BgtButton variant="cancel" size="1" onClick={confirmModal.show} disabled={sendInvitesMutation.isPending}>
					{t("card.email-invites")}
				</BgtButton>
			)}
			<BgtEditDeleteButtons onEdit={() => onEdit(gameNight)} onDelete={() => onDelete(gameNight)} />
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
		</div>
	);
};
