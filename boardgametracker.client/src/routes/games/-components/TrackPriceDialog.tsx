import { type FormEvent, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import Target from "@/assets/icons/target.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtDialog, BgtDialogContent, BgtDialogDescription, BgtDialogTitle } from "@/components/BgtDialog/BgtDialog";
import { BgtSimpleInputField } from "@/components/BgtForm";

interface Props {
	open: boolean;
	close: () => void;
	initialUrl: string | null;
	isPending: boolean;
	onSubmit: (url: string) => Promise<unknown>;
}

const HTTP_URL = /^https?:\/\/.+/i;

export const TrackPriceDialog = ({ open, close, initialUrl, isPending, onSubmit }: Props) => {
	const { t } = useTranslation(["game", "common"]);
	const [url, setUrl] = useState(initialUrl ?? "");

	useEffect(() => {
		if (open) {
			setUrl(initialUrl ?? "");
		}
	}, [open, initialUrl]);

	const trimmed = url.trim();
	const valid = HTTP_URL.test(trimmed);

	const submit = async (event: FormEvent) => {
		event.preventDefault();
		if (!valid) return;
		await onSubmit(trimmed);
	};

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent className="max-w-lg">
				<BgtDialogTitle className="flex items-center gap-2">
					<Target className="size-5 text-primary" />
					{t("game:track-price.title")}
				</BgtDialogTitle>
				<BgtDialogDescription>{t("game:track-price.description")}</BgtDialogDescription>
				<form onSubmit={submit} className="flex flex-col gap-3 pt-2">
					<BgtSimpleInputField
						type="text"
						label={t("game:track-price.url-label")}
						value={url}
						onChange={(event) => setUrl(event.target.value)}
						placeholder={t("game:track-price.url-placeholder")}
						disabled={isPending}
					/>
					<div className="flex justify-end gap-2">
						<BgtButton variant="cancel" type="button" onClick={close} disabled={isPending}>
							{t("common:cancel")}
						</BgtButton>
						<BgtButton variant="primary" type="submit" disabled={!valid || isPending}>
							{t("game:track-price.submit")}
						</BgtButton>
					</div>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
