import { type FormEvent, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import Package from "@/assets/icons/package.svg?react";
import Trash from "@/assets/icons/trash.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtSimpleInputField, BgtSimpleSelect } from "@/components/BgtForm";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { BgtText } from "@/components/BgtText/BgtText";
import { useDecimalInput } from "@/hooks/useDecimalInput";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { type Expansion, GameState } from "@/models";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";

interface Props {
	gamePrice: number | null;
	expansions: Expansion[];
	open: boolean;
	close: () => void;
	canWrite: boolean;
	bggEnabled: boolean;
	currency?: string;
	onAddExpansion: () => void;
	onAddManualExpansion: (title: string) => Promise<boolean>;
	onDeleteExpansion: (expansionId: number) => void;
	onUpdateExpansion: (expansion: Expansion) => void;
}

interface PriceInputProps {
	value: number | null;
	onCommit: (value: number | null) => void;
	ariaLabel: string;
	placeholder: string;
	currency?: string;
	prefix?: string;
}

const PriceInput = ({ value, onCommit, ariaLabel, placeholder, currency, prefix }: PriceInputProps) => {
	const [current, setCurrent] = useState<number | null>(value);
	useEffect(() => setCurrent(value), [value]);
	const decimal = useDecimalInput(current, setCurrent);

	return (
		<BgtSimpleInputField
			type="text"
			inputMode="decimal"
			value={decimal.value}
			onChange={(event) => decimal.onChange(event.target.value)}
			onBlur={() => {
				decimal.onBlur();
				if (current !== value && (current === null || current >= 0)) {
					onCommit(current);
				}
			}}
			placeholder={placeholder}
			prefixLabel={prefix}
			suffixLabel={currency}
			ariaLabel={ariaLabel}
			className="w-full"
		/>
	);
};

export const ExpansionsDialog = (props: Props) => {
	const {
		gamePrice,
		expansions,
		open,
		close,
		canWrite,
		bggEnabled,
		currency,
		onAddExpansion,
		onAddManualExpansion,
		onDeleteExpansion,
		onUpdateExpansion,
	} = props;
	const { t } = useTranslation(["game", "common"]);
	const { price } = useNumberFormat();
	const [title, setTitle] = useState("");
	const [saving, setSaving] = useState(false);

	const stateItems = Object.values(GameState).map((value) => ({
		value,
		label: t(getItemStateTranslationKey(value, false)),
	}));
	const ownedExpansionsPrice = expansions
		.filter((expansion) => expansion.state === GameState.Owned)
		.reduce((sum, expansion) => sum + (expansion.buyingPrice ?? 0), 0);
	const formatPrice = (value: number) => price(value, currency ?? "");

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
			<BgtDialogContent size="wide">
				<BgtDialogTitle className="flex items-center gap-2">
					<Package className="size-5 text-primary" />
					{`${t("expansions.title")} (${expansions.length})`}
				</BgtDialogTitle>
				<BgtDialogDescription>{t("expansions.description")}</BgtDialogDescription>
				<div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_16rem]">
					{expansions.length === 0 ? (
						<div className="py-8 text-center text-sm text-white/50">{t("expansions.none")}</div>
					) : (
						<ul className="flex flex-col gap-2 lg:max-h-[55vh] lg:overflow-y-auto lg:pr-1">
							{expansions.map((expansion) => (
								<li
									key={expansion.id}
									className="grid grid-cols-[2.5rem_minmax(0,1fr)_auto] items-center gap-x-3 gap-y-2 rounded-lg border border-primary/10 bg-primary/5 p-3 sm:grid-cols-[2.5rem_minmax(0,1fr)_9rem_7.5rem_auto]"
								>
									<BgtAvatar image={expansion.image} title={expansion.title} size="large" />
									<div className="min-w-0">
										<BgtText color="white" className="truncate">
											{expansion.title}
										</BgtText>
										{expansion.bggId === null && (
											<BgtText size="1" color="gray">
												{t("expansions.manual.badge")}
											</BgtText>
										)}
									</div>
									{canWrite ? (
										<div className="col-span-3 col-start-1 row-start-2 grid grid-cols-2 gap-2 sm:col-span-2 sm:col-start-3 sm:row-start-1 sm:grid-cols-[9rem_7.5rem]">
											<BgtSimpleSelect
												items={stateItems}
												value={expansion.state}
												aria-label={`${t("state.label")}: ${expansion.title}`}
												onValueChange={(state) => onUpdateExpansion({ ...expansion, state: state as GameState })}
											/>
											<PriceInput
												value={expansion.buyingPrice}
												onCommit={(buyingPrice) => onUpdateExpansion({ ...expansion, buyingPrice })}
												ariaLabel={`${t("price.label")}: ${expansion.title}`}
												placeholder={t("price.placeholder")}
												currency={currency}
											/>
											{expansion.state === GameState.Sold && (
												<div className="col-span-2 col-start-1 sm:col-start-2 sm:col-span-1">
													<PriceInput
														value={expansion.soldPrice}
														onCommit={(soldPrice) => onUpdateExpansion({ ...expansion, soldPrice })}
														ariaLabel={`${t("update.sold-price.label")}: ${expansion.title}`}
														placeholder={t("update.sold-price.label")}
														currency={currency}
														prefix={t("state.sold")}
													/>
												</div>
											)}
										</div>
									) : (
										<dl className="col-span-2 col-start-2 row-start-2 flex flex-wrap gap-x-4 gap-y-1 text-sm sm:col-span-2 sm:col-start-3 sm:row-start-1 sm:justify-end">
											<div className="flex gap-1">
												<dt className="sr-only">{t("state.label")}</dt>
												<dd className="text-white/80">{t(getItemStateTranslationKey(expansion.state, false))}</dd>
											</div>
											<div className="flex gap-1">
												<dt className="sr-only">{t("price.label")}</dt>
												<dd className="font-bold tabular-nums text-card-value">
													{expansion.buyingPrice == null ? "-" : formatPrice(expansion.buyingPrice)}
												</dd>
											</div>
											{expansion.state === GameState.Sold && expansion.soldPrice != null && (
												<div className="flex gap-1">
													<dt className="text-white/60">{t("update.sold-price.label")}</dt>
													<dd className="font-bold tabular-nums text-card-value">{formatPrice(expansion.soldPrice)}</dd>
												</div>
											)}
										</dl>
									)}
									{canWrite && (
										<div className="col-start-3 row-start-1 sm:col-start-5">
											<BgtIconButton
												icon={<Trash />}
												intent="danger"
												onClick={() => onDeleteExpansion(expansion.id)}
												aria-label={`${t("common:delete.button")}: ${expansion.title}`}
											/>
										</div>
									)}
								</li>
							))}
						</ul>
					)}
					<aside className="flex flex-col gap-4">
						<dl className="grid grid-cols-[1fr_auto] gap-x-4 gap-y-1 rounded-lg border border-primary/20 bg-primary/10 p-3 text-sm">
							<dt className="text-white/70">{t("expansions.prices.base")}</dt>
							<dd className="text-right font-bold tabular-nums text-card-value">{formatPrice(gamePrice ?? 0)}</dd>
							<dt className="text-white/70">{t("expansions.prices.expansions")}</dt>
							<dd className="text-right font-bold tabular-nums text-card-value">{formatPrice(ownedExpansionsPrice)}</dd>
							<dt className="mt-1 border-t border-primary/20 pt-1 text-white/70">{t("expansions.prices.together")}</dt>
							<dd className="mt-1 border-t border-primary/20 pt-1 text-right font-bold tabular-nums text-card-value">
								{formatPrice((gamePrice ?? 0) + ownedExpansionsPrice)}
							</dd>
						</dl>
						{canWrite && (
							<form onSubmit={submitManual} className="flex flex-col gap-2">
								<BgtText size="2" color="gray">
									{t("expansions.manual.description")}
								</BgtText>
								<div className="flex items-end gap-2">
									<BgtSimpleInputField
										type="text"
										value={title}
										placeholder={t("expansions.manual.placeholder")}
										ariaLabel={t("expansions.manual.placeholder")}
										onChange={(event) => setTitle(event.target.value)}
										disabled={saving}
									/>
									<BgtButton type="submit" variant="primary" disabled={saving || !title.trim()}>
										{t("expansions.manual.add")}
									</BgtButton>
								</div>
							</form>
						)}
					</aside>
				</div>
				<BgtDialogFooter
					onCancel={close}
					cancelLabel={t("common:close")}
					confirmLabel={canWrite && bggEnabled ? t("expansions.add") : undefined}
					onConfirm={onAddExpansion}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
