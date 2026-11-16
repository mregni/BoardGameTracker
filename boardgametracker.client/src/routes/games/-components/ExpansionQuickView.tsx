import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import DicesIcon from "@/assets/icons/dices.svg?react";
import PackageIcon from "@/assets/icons/package.svg?react";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtFieldLabel, BgtSimpleSelect } from "@/components/BgtForm";
import { BgtFallbackImage } from "@/components/BgtImage/BgtFallbackImage";
import { ExpansionStorageField } from "@/components/BgtStorage/ExpansionStorageField";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { type Expansion, type Game, GameState } from "@/models";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { useInlineExpansionUpdate } from "../-hooks/useInlineExpansionUpdate";
import { PriceInput } from "./PriceInput";

interface Props {
	game: Game;
	expansion: Expansion;
	canWrite: boolean;
	currency?: string;
	close: () => void;
}

export const ExpansionQuickView = ({ game, expansion, canWrite, currency, close }: Props) => {
	const { t } = useTranslation(["game", "games", "storage", "common"]);
	const { price } = useNumberFormat();
	const { updateExpansion } = useInlineExpansionUpdate();
	const stateItems = Object.values(GameState).map((value) => ({
		value,
		label: t(getItemStateTranslationKey(value, false)),
	}));

	return (
		<BgtDialog open onClose={close}>
			<BgtDialogContent size="wide">
				<BgtDialogTitle className="flex items-center gap-2">
					<PackageIcon className="size-5 text-primary" aria-hidden="true" />
					{expansion.title}
				</BgtDialogTitle>
				<BgtDialogDescription>
					{t("games:quick-view.expansion-of")}{" "}
					<Link
						to="/games/$gameId"
						params={{ gameId: game.id }}
						className="text-primary-text underline-offset-4 hover:underline"
					>
						{game.title}
					</Link>
				</BgtDialogDescription>
				<div className="grid gap-5 md:grid-cols-[12rem_minmax(0,1fr)]">
					<div className="aspect-square w-32 overflow-hidden rounded-lg md:w-full">
						<BgtFallbackImage title={expansion.title} image={expansion.image} decorative />
					</div>
					<div className="flex flex-col gap-4">
						{canWrite ? (
							<div className="grid grid-cols-2 gap-3">
								<BgtSimpleSelect
									label={t("game:state.label")}
									items={stateItems}
									value={expansion.state}
									onValueChange={(state) => updateExpansion(game.id, { ...expansion, state: state as GameState })}
								/>
								<div className="flex flex-col">
									<BgtFieldLabel>{t("game:price.label")}</BgtFieldLabel>
									<PriceInput
										value={expansion.buyingPrice}
										onCommit={(buyingPrice) => updateExpansion(game.id, { ...expansion, buyingPrice })}
										ariaLabel={t("game:price.label")}
										placeholder={t("game:price.placeholder")}
										currency={currency}
									/>
								</div>
							</div>
						) : (
							<dl className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-4 gap-y-1 text-sm">
								<dt className="text-white/60">{t("game:state.label")}</dt>
								<dd>{t(getItemStateTranslationKey(expansion.state, false))}</dd>
								<dt className="text-white/60">{t("game:price.label")}</dt>
								<dd className="font-bold tabular-nums text-card-value">
									{expansion.buyingPrice == null ? "-" : price(expansion.buyingPrice, currency ?? "")}
								</dd>
							</dl>
						)}
						<div className="flex flex-col gap-1">
							<BgtFieldLabel>{t("storage:picker.label")}</BgtFieldLabel>
							<ExpansionStorageField expansion={expansion} baseTitle={game.title} canWrite={canWrite} />
						</div>
					</div>
				</div>
				<div className="flex flex-wrap items-center justify-between gap-3">
					<Link
						to="/games/$gameId"
						params={{ gameId: game.id }}
						className="inline-flex items-center gap-1.5 text-sm text-primary-text underline-offset-4 hover:underline"
					>
						<DicesIcon className="size-4" aria-hidden="true" />
						{t("games:quick-view.open", { title: game.title })}
					</Link>
					<BgtDialogFooter onCancel={close} cancelLabel={t("common:close")} className="mt-0" />
				</div>
			</BgtDialogContent>
		</BgtDialog>
	);
};
