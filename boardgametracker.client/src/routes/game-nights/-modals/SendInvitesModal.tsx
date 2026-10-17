import { useTranslation } from "react-i18next";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtText } from "@/components/BgtText/BgtText";
import type { ModalProps } from "@/models";
import type { InviteRecipients } from "../-utils/inviteRecipients";

interface Props extends ModalProps {
	title: string;
	recipients: InviteRecipients;
	isPending: boolean;
	onConfirm: () => void;
}

export const SendInvitesModal = (props: Props) => {
	const { open, close, title, recipients, isPending, onConfirm } = props;
	const { t } = useTranslation(["game-nights", "common"]);
	const count = recipients.withEmail.length;

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<BgtDialogTitle>{t("invites.title")}</BgtDialogTitle>
				<BgtDialogDescription>
					{count > 0 ? t("invites.description", { count, title }) : t("invites.nobody", { title })}
				</BgtDialogDescription>
				{count > 0 && (
					<BgtText size="2" data-testid="invite-recipients">
						{recipients.withEmail.join(", ")}
					</BgtText>
				)}
				{recipients.withoutEmail.length > 0 && (
					<BgtText size="2" color="gray" data-testid="invite-without-email">
						{t("invites.without-email", {
							count: recipients.withoutEmail.length,
							names: recipients.withoutEmail.join(", "),
						})}
					</BgtText>
				)}
				<BgtDialogFooter
					onCancel={close}
					confirmLabel={t("invites.send", { count })}
					onConfirm={onConfirm}
					disabled={count === 0}
					loading={isPending}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
