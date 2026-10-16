import { useState } from "react";
import { useTranslation } from "react-i18next";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";

interface Props {
	open: boolean;
	close: () => void;
	username: string;
	tempPassword: string;
}

export const TempPasswordModal = ({ open, close, username, tempPassword }: Props) => {
	const { t } = useTranslation(["settings", "common"]);
	const [copied, setCopied] = useState(false);

	const handleCopy = async () => {
		await navigator.clipboard.writeText(tempPassword);
		setCopied(true);
		setTimeout(() => setCopied(false), 2000);
	};

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<BgtDialogTitle>{t("account.users.temp-password.title")}</BgtDialogTitle>
				<BgtDialogDescription>{t("account.users.temp-password.description", { username })}</BgtDialogDescription>
				<div className="my-4 rounded-lg bg-white/5 border border-white/10 p-4">
					<code className="text-lg font-mono text-white break-all">{tempPassword}</code>
				</div>
				<BgtDialogFooter
					onCancel={close}
					cancelLabel={t("common:close")}
					confirmLabel={copied ? t("account.users.temp-password.copied") : t("account.users.temp-password.copy")}
					onConfirm={handleCopy}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
