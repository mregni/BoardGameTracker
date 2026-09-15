import { type FormEvent, useState } from "react";
import { useTranslation } from "react-i18next";
import Package from "@/assets/icons/package.svg?react";
import Trash from "@/assets/icons/trash.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtDialog, BgtDialogContent, BgtDialogDescription, BgtDialogTitle } from "@/components/BgtDialog/BgtDialog";
import { BgtSimpleInputField } from "@/components/BgtForm";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { BgtText } from "@/components/BgtText/BgtText";

interface Expansion {
	id: number;
	title: string;
	bggId: number | null;
}

interface Props {
	expansions: Expansion[];
	open: boolean;
	close: () => void;
	canWrite: boolean;
	bggEnabled: boolean;
	onAddExpansion: () => void;
	onAddManualExpansion: (title: string) => Promise<boolean>;
	onDeleteExpansion: (expansionId: number) => void;
}

export const ExpansionsDialog = (props: Props) => {
	const { expansions, open, close, canWrite, bggEnabled, onAddExpansion, onAddManualExpansion, onDeleteExpansion } =
		props;
	const { t } = useTranslation("game");
	const [title, setTitle] = useState("");
	const [saving, setSaving] = useState(false);

	const submitManual = async (event: FormEvent<HTMLFormElement>) => {
		event.preventDefault();
		const trimmed = title.trim();
		if (!trimmed) return;
		setSaving(true);
		try {
			if (await onAddManualExpansion(trimmed)) {
				setTitle("");
			}
		} finally {
			setSaving(false);
		}
	};

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent className="max-w-lg">
				<BgtDialogTitle className="flex items-center gap-2">
					<Package className="size-5 text-primary" />
					{`${t("expansions.title")} (${expansions.length})`}
				</BgtDialogTitle>
				<BgtDialogDescription>{t("expansions.description")}</BgtDialogDescription>
				{expansions.length === 0 ? (
					<div className="text-center py-8">
						<div className="text-white/50 text-sm">{t("expansions.none")}</div>
					</div>
				) : (
					<div className="space-y-2 py-2 max-h-[60vh] overflow-y-auto">
						{expansions.map((expansion) => (
							<div
								key={expansion.id}
								className="flex items-center gap-3 bg-primary/5 rounded-lg p-4 border border-primary/10 group"
							>
								<div className="shrink-0 w-10 h-10 bg-primary/20 rounded-lg flex items-center justify-center border border-primary/30">
									<Package className="text-primary" />
								</div>
								<div className="flex-1 min-w-0">
									<BgtText color="white">{expansion.title}</BgtText>
									{expansion.bggId === null && (
										<BgtText size="1" color="gray">
											{t("expansions.manual.badge")}
										</BgtText>
									)}
								</div>
								{canWrite && (
									<div className="flex">
										<BgtIconButton
											icon={<Trash />}
											intent="danger"
											onClick={() => onDeleteExpansion(expansion.id)}
											aria-label={t("common:delete.button")}
										/>
									</div>
								)}
							</div>
						))}
					</div>
				)}
				{canWrite && (
					<form onSubmit={submitManual} className="flex flex-col gap-2 pt-2 border-t border-primary/10">
						<BgtText size="2" color="gray">
							{t("expansions.manual.description")}
						</BgtText>
						<div className="flex gap-2 items-end">
							<BgtSimpleInputField
								type="text"
								value={title}
								placeholder={t("expansions.manual.placeholder")}
								onChange={(event) => setTitle(event.target.value)}
								disabled={saving}
							/>
							<BgtButton type="submit" variant="primary" disabled={saving || !title.trim()}>
								{t("expansions.manual.add")}
							</BgtButton>
						</div>
					</form>
				)}
				{canWrite && bggEnabled && (
					<div className="flex justify-end pt-2">
						<BgtButton onClick={onAddExpansion}>{t("expansions.add")}</BgtButton>
					</div>
				)}
			</BgtDialogContent>
		</BgtDialog>
	);
};
